// ~-----libraries------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
#include "stdafx.h"
#include "Interpolator.h"
#include "Synchronizer.h"


// ~-----namespaces-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
using namespace std;



// ~-----local function prototypes--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
void combinations_of_cluster(Coordinates& considered_point, int depth, vector<string>& couple_IDs, multimap<string, Coordinates>& intersection_all, double sum, double& min_sum);
void discard_empty_packet(Packet& packet);



// ~-----constructors and destructors-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
Interpolator::Interpolator(string configuration_id,
	map<std::string, Coordinates> boards_position,
	std::vector<Coordinates> ordered_boards,
	std::shared_ptr<BlockingQueue_Interpolator> queue_input_ptr) :
		_configuration_id(configuration_id), _queue_input(queue_input_ptr), _vertices_ordered(ordered_boards), _boards_position(boards_position)
{
	// ~-----order vertices-------------------------------------------------
	vector<Coordinates> unordered_points;
	transform(boards_position.begin(), boards_position.end(), back_inserter(unordered_points), [](const auto& val) {return val.second; });

	//_vertices_ordered = Coordinates::sort(unordered_points);
}



// ~-----methods--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

/* Nicolò:
 * interpolate the position of the detected smartphone,
 * given the list of the couples:
 *   - Coordinates (x:y) of a board
 *   - RSSI (signal power) of a board listening
 */
Coordinates Interpolator::interpolate_position(Packet& packet) {
	// ~-----local variables------------------------------------------------
	map<string, double> distances;
	multimap<string, Coordinates> intersections_all;
	map<string, Coordinates> intersections_filtered;
	vector<string> couple_IDs;
	map<Coordinates, double> intersections_weight;
	double weight_sum = 0, x_sum = 0, y_sum = 0;

	// ~-----check input parameter------------------------------------------
	discard_empty_packet(packet);	// can throw Interpolation_Exception

#if DEBUG
	Statistics::write_room(packet);
	if (Statistics::is_interesting(packet)) {
		Statistics::write_RSSI_vs_distance(packet, _boards_position);
	}
	Statistics::start_timer_performance();
#endif

	// ~-----translate signal powers (RSSIs) in distance (meters)-----------
	for (auto iterator : packet.RSSIs()) {
		distances.insert(make_pair(iterator.first, convert_RSSI_to_meter(iterator.second)));
	}
#if DEBUG
	Statistics::write_circumference(packet, distances);
#endif


	// ~-----find all the points of the discrete distribution---------------
	for (auto iterator_external = distances.begin(); iterator_external != distances.end(); iterator_external++) {
		// first circle data
		auto center1 = _boards_position.find(iterator_external->first)->second;
		double r1 = distances.find(iterator_external->first)->second;

		for (auto iterator_internal = next(iterator_external, 1); iterator_internal != distances.end(); iterator_internal++) {
			// intersection points
			Coordinates intersection1, intersection2;
			// second circle data
			auto center2 = _boards_position.find(iterator_internal->first)->second;
			double r2 = distances.find(iterator_internal->first)->second;
			// couple identifier (concatenation of MACs)
			string couple_ID = iterator_external->first + "-" + iterator_internal->first;


			// compute the TWO intersections
			try
			{
				circle_intersections(center1, r1, center2, r2, intersection1, intersection2);
			}
			catch (const std::exception& e)
			{
#if DEBUG
				// console output
				cerr << "Impossible to compute intersection between "
					<< "(x-" << center1.x() << ")^2 + (y-" << center1.y() << ")^2 = (" << r1 << ")^2 and "
					<< "(x-" << center2.x() << ")^2 + (y-" << center2.y() << ")^2 = (" << r2 << ")^2 "
					<< "due to:" << endl << "\t" << e.what() << endl;

#endif
				// discard current Packet (do NOT insert in the database)
				throw Interpolation_Exception("Impossible to determine some intersections");
			}

			// store the found intersections
			couple_IDs.push_back(couple_ID);
			intersections_all.insert(make_pair(couple_ID, intersection1));
			intersections_all.insert(make_pair(couple_ID, intersection2));
		}
	}


	// ~-----check if there are intersections-------------------------------
	if (intersections_all.size() == 0) {
		throw Interpolation_Exception("There are no intersections");
	}
	else if (intersections_all.size() == 1) {
		return intersections_all.begin()->second;
	}
	else if (intersections_all.size() == 2) {
		Coordinates first = intersections_all.begin()->second;
		Coordinates second = next(intersections_all.begin(), 1)->second;

		Coordinates gravity_center((first.x() + second.x()) / 2, (first.y() + second.y()) / 2);
#if DEBUG
		chrono::microseconds stop2 = Statistics::current_timer_performance();
		_m_console.lock();
		std::cout << "Device " << packet.MACaddress_device() << " detected in ( " << gravity_center.x() << " : " << gravity_center.y() << " )\t" << flush << endl;
		_m_console.unlock();
		Statistics::write_intersections2(packet, first, second);
		Statistics::write_detected(packet, gravity_center);
#endif
		if (!is_between_points(gravity_center)) {
#if DEBUG
			Statistics::write_position(packet, gravity_center, _boards_position.size(), to_string(_algorithm), stop2, false);
#endif
			throw Interpolation_Exception("The detected smartphone is outside the room");
		}
#if DEBUG
		Statistics::write_position(packet, gravity_center, _boards_position.size(), to_string(_algorithm), stop2, true);
#endif

		return gravity_center;
	}


	// ~-----discard aberrant data (compute distance sum)-------------------
	switch (_algorithm) 
	{
	case Interpolator::fast_unordered:
		intersections_filtered = clusterize_fast(couple_IDs, intersections_all);
		break;
	case Interpolator::hybrid_unordered:
		intersections_filtered = clusterize_hybrid(couple_IDs, intersections_all);
		break;
	case Interpolator::accurate_ordered:
		intersections_filtered = clusterize_accurate(couple_IDs, intersections_all, distances);
		break;
	default:
		intersections_filtered = clusterize_fast(couple_IDs, intersections_all);
		break;
	}


	// ~-----gravity center of the discrete distribution--------------------
	// compute a weighted average (w.r.t. the circle radius) on both axis
	for (auto intersection : intersections_filtered) {
		// compute the weight: retrieve both radius in the boards couple
		string MAC1 = intersection.first.substr(0, intersection.first.find("-"));
		string MAC2 = intersection.first.substr(intersection.first.find("-") + 1, intersection.first.length() - 1);
		double weight = 1 / (distances[MAC1] + distances[MAC2]);	// weight_i = 1 / (radius_i1 + radius_i2)

		// apply the weight: favor the smaller circles
		x_sum += weight * intersection.second.x();
		y_sum += weight * intersection.second.y();
		weight_sum += weight;
	}

	// compute the detected position of the original device
	Coordinates gravity_center(x_sum / weight_sum, y_sum / weight_sum);

#if DEBUG
	chrono::microseconds stop = Statistics::current_timer_performance();

	_m_console.lock();
	std::cout << "Device " << packet.MACaddress_device() << " detected in ( " << gravity_center.x() << " : " << gravity_center.y() << " )\t" << flush << endl;
	_m_console.unlock();

	Statistics::write_intersections(packet, intersections_all, intersections_filtered);
	Statistics::write_detected(packet, gravity_center);
#endif

	// check if it is inside/outside the room
	if ( !is_inside_perimeter(gravity_center) ) {
#if DEBUG
		if (Statistics::is_interesting(packet))
			Statistics::write_position(packet, gravity_center, _boards_position.size(), to_string(_algorithm), stop, false);
#endif
		throw Interpolation_Exception("The detected smartphone is outside the room");
	}
#if DEBUG
	if (Statistics::is_interesting(packet))
		Statistics::write_position(packet, gravity_center, _boards_position.size(), to_string(_algorithm), stop, true);
#endif

	return gravity_center;
}


/* Nicolò:
 * streaming clustering algorithm to determine the interesting intersection in each pair
 * lighter (less computations) but faster (more performance) version, with the following features:
 *    - unordered: does not sort the incoming intersections
 *    - without elimination: keeps the intersections discarded in the previous iterations
 */
map<string, Coordinates> Interpolator::clusterize_fast(vector<string> couple_IDs, multimap<string, Coordinates> intersections_all) {
	// ~-----local variables------------------------------------------------
	map<string, Coordinates> intersections_filtered;

	// ~-----filter the useless intersection in each pair-------------------
	for (string couple : couple_IDs) {
		// support information
		double distance_sum1 = 0, distance_sum2 = 0;
		Coordinates point1 = intersections_all.find(couple)->second;
		Coordinates point2 = next(intersections_all.find(couple), 1)->second;

		// compute the sum of the distance with respect to all the other points
		for (auto intersection : intersections_all) {
			if (intersection.first != couple) {
				distance_sum1 += point1.distance(intersection.second);
				distance_sum2 += point2.distance(intersection.second);
			}
		}

		// keep only the nearest
		if (distance_sum1 < distance_sum2)
			intersections_filtered.insert(make_pair(couple, point1));
		else
			intersections_filtered.insert(make_pair(couple, point2));
	}

	// ~-----return the list of the useful intersections--------------------
	return intersections_filtered;
}


/* Nicolò:
 * streaming clustering algorithm to determine the interesting intersection in each pair
 * intermediate version, with the following features:
 *    - unordered: does not sort the incoming intersections
 *    - with elimination: at each iteration discard the not useful intersection
 */
map<string, Coordinates> Interpolator::clusterize_hybrid(vector<string> couple_IDs, multimap<string, Coordinates> intersections_all) {
	// ~-----local variables------------------------------------------------
	map<string, Coordinates> intersections_filtered;

	// ~-----filter the useless intersection in each pair-------------------
	for (string couple : couple_IDs) {
		// support information
		double distance_sum1 = 0, distance_sum2 = 0;
		Coordinates point1 = intersections_all.find(couple)->second;
		Coordinates point2 = next(intersections_all.find(couple), 1)->second;

		// compute the sum of the distance with respect to the remaining other points
		for (auto intersection : intersections_all) {
			if (intersection.first != couple) {
				distance_sum1 += point1.distance(intersection.second);
				distance_sum2 += point2.distance(intersection.second);
			}
		}

		// keep the nearest point and discard the other one
		if (distance_sum1 < distance_sum2) {
			intersections_filtered.insert(make_pair(couple, point1));
			intersections_all.erase(next(intersections_all.find(couple), 1));
		}
		else {
			intersections_filtered.insert(make_pair(couple, point2));
			intersections_all.erase(intersections_all.find(couple));
		}
	}

	// ~-----return the list of the useful intersections--------------------
	return intersections_filtered;
}


/* Nicolò:
 * streaming clustering algorithm to determine the interesting intersection in each pair
 * heavier (more computations) but more precise (less error) version, with the following features:
 *    - ordered: sort the incoming intersections according to the radius of the circumferences from which they are calculated
 *    - with elimination: at each iteration discard the not useful intersection
 */
map<string, Coordinates> Interpolator::clusterize_accurate(vector<string> couple_IDs, multimap<string, Coordinates> intersections_all, map<string, double> distances) {
	// ~-----local variables------------------------------------------------
	map<string, Coordinates> intersections_filtered;

	// ~-----sort the circle couple according to the bigger radiuses sum----
	sort(couple_IDs.begin(),
		couple_IDs.end(),
		[&](string a, string b) {
			string delimiter = "-";
			string a_MAC1 = a.substr(0, a.find(delimiter));
			string a_MAC2 = a.substr(a.find(delimiter) + 1, a.length() - 1);
			string b_MAC1 = b.substr(0, b.find(delimiter));
			string b_MAC2 = b.substr(b.find(delimiter) + 1, b.length() - 1);

			double a_distancesum = distances[a_MAC1] + distances[a_MAC2];
			double b_distancesum = distances[b_MAC1] + distances[b_MAC2];
			return a_distancesum > b_distancesum;
		}
	);

	// ~-----filter the useless intersection in each pair-------------------
	for (string couple : couple_IDs) {
		// support information
		double distance_sum1 = numeric_limits<double>::max(), distance_sum2 = numeric_limits<double>::max();
		Coordinates point1 = intersections_all.find(couple)->second;
		Coordinates point2 = next(intersections_all.find(couple), 1)->second;

		// compute all the combinations of intersection_points and keep the distance_sum w.r.t. the best cluster
		combinations_of_cluster(point1, 0, couple_IDs, intersections_all, 0, distance_sum1);
		combinations_of_cluster(point2, 0, couple_IDs, intersections_all, 0, distance_sum2);

		// keep the nearest point and discard the other one
		if (distance_sum1 < distance_sum2) {
			intersections_filtered.insert(make_pair(couple, point1));
			if (next(intersections_all.find(couple), 1) == intersections_all.end())
				cout << "";
			intersections_all.erase(next(intersections_all.find(couple), 1));
		}
		else {
			intersections_filtered.insert(make_pair(couple, point2));
			if (intersections_all.find(couple) == intersections_all.end())
				cout << "";
			intersections_all.erase(intersections_all.find(couple));
		}
	}

	// ~-----return the list of the useful intersections--------------------
	return intersections_filtered;
}


void combinations_of_cluster(Coordinates& considered_point, int depth, vector<string>& couple_IDs, multimap<string, Coordinates>& intersection_all, double sum, double& min_sum) {
	// local variables
	string curr_couple;
	Coordinates comparison_point;

	//exiting condition
	if (depth == couple_IDs.size() || sum > min_sum) {
		if (sum < min_sum)
			min_sum = sum;
		return;
	}

	//define current couple
	curr_couple = couple_IDs[depth];

	//do not consider the same point
	if ((intersection_all.find(curr_couple)->second == considered_point && intersection_all.find(curr_couple) != intersection_all.end())
		|| (next(intersection_all.find(curr_couple), 1) != intersection_all.end() && next(intersection_all.find(curr_couple), 1)->second == considered_point)) {
		depth++;
		combinations_of_cluster(considered_point, depth, couple_IDs, intersection_all, sum, min_sum); //no changes on sum
		return;
	}

	//internal loop (iterate only on 2 points or 1 if the other was already discarder)
	for (auto iter = intersection_all.lower_bound(curr_couple); iter != intersection_all.upper_bound(curr_couple); iter++) {
		comparison_point = iter->second;
		double temp_sum = sum + considered_point.distance(comparison_point);
		int temp_depth = depth + 1;
		combinations_of_cluster(considered_point, temp_depth, couple_IDs, intersection_all, temp_sum, min_sum);
	}
}

void discard_empty_packet(Packet& packet) {
	// local variables
	string message = "Incoming packet is empty: missing ";
	bool something_empty = false;

	// check total
	if ( packet.empty() ) {
		message.append("everything!");
		throw Interpolation_Exception(message);
	}

	// check single fields
	if (packet.hash() == 0) {
		message.append("hash code, ");
		something_empty = true;
	}
	if (packet.MACaddress_device().empty()) {
		message.append("MAC address device, ");
		something_empty = true;
	}
	if (packet.timestamps().empty()) {
		message.append("timestamp list, ");
		something_empty = true;
	}
	if (packet.RSSIs().empty()) {
		message.append("RSSI list, ");
		something_empty = true;
	}
	message = message.substr(0, message.length() - 2);

	if (something_empty)
		throw Interpolation_Exception(message);
}


/* Nicolò:
 * calculate the timestamp at which the WiFi packet was originally sent,
 * given the list of the couples:
 *   - Coordinates (x:y) of a board
 *   - timestamp of a board listening
 */
chrono::milliseconds Interpolator::calculate_timestamp(Packet& packet) {
	// ~-----local variables------------------------------------------------
	chrono::milliseconds sum, output;
	double weight, numerator = 0, denumerator = 0;


	// ~-----calculate weighted average value-------------------------------
	for (auto iterator : packet.timestamps()) {
		// variable initialization
		sum = chrono::milliseconds::zero();
		weight = 0;
		
		// compute distance with respect to all other detections
		for (auto iterator_inner : packet.timestamps()) {
			if (iterator_inner.first != iterator.first) {
				if (iterator.second > iterator_inner.second)
					sum += iterator.second - iterator_inner.second;
				else
					sum += iterator_inner.second - iterator.second;
			}
		}

		// compute weight
		if (sum.count() == 0)	// all identical timestamps
			weight = 1;
		else
			weight = 1 / (double)sum.count();	// w = 1 / Σ(d)
		numerator += weight * (double)iterator.second.count();
		denumerator += weight;
	}
	// cast the average to timestamp
	output = chrono::milliseconds( (uint64_t)(numerator / denumerator) );
	if (output.count() < 0)
		throw Interpolation_Exception("The detected timestamp is not valid (i.e. negative)");


	// ~-----return estimated timestamp (if not in the future)--------------
	chrono::milliseconds now = chrono::duration_cast<chrono::milliseconds>(chrono::system_clock::now().time_since_epoch());
	if (output > now)
		return now;
	else
		return output;
}


/* Nicolò:
 * convert the power RSSI of a WiFi signal (received by a board),
 * in its corresponding distance (in meters)
 */
double Interpolator::convert_RSSI_to_meter(double RSSI) {
	// ~-----local variables------------------------------------------------
	double Ptx;			// transmission power - usually ranges between -59 to -65
	double n;			// multiplicative coefficient - usually 2
	double distance;	// desired output


	// ~-----set conversion parameters--------------------------------------
	switch (_parameters) {
	case literature:
		n = 1.80;
		Ptx = -59;
		break;
	case heuristic:
		n = 2.45;
		Ptx = -52.78;
		break;
	case regression:
		n = 3.20;
		Ptx = -47.91;
		break;
	}
#if DEBUG
	Statistics::set_conversion_coefficients(Ptx, n);
#endif


	// ~-----check for solvability------------------------------------------
	if (RSSI >= 0)
		throw Interpolation_Exception("Incorrect value of RSSI");


	// ~-----compute distance-----------------------------------------------
	/* theoretical formulas:
	 *    RSSI_received = -n * 10 * log10(distance) + Ptx
	 *    RSSI_AtOneMeter = TxPower - 62
	 * used formula (from the first, above):
	 *    distance = 10 ^ ((Ptx - RSSI_received) / (n * 10))
	 */
	distance = pow(10, (Ptx - RSSI) / (n * 10));

	return distance;
}


/* Nicolò:
 * calculate the two coordinates of the intersection of two
 * bidimensional circumferences
 */
void Interpolator::circle_intersections(Coordinates circle_center1, double radius1,
	Coordinates circle_center2, double radius2,
	Coordinates& intersection1, Coordinates& intersection2)
{
	// ~-----local variables------------------------------------------------
	double a, dx, dy, d, h, rx, ry;
	double x3, y3;

	// dx and dy are the vertical and horizontal distances between the circle centers
	dx = circle_center2.x() - circle_center1.x();
	dy = circle_center2.y() - circle_center1.y();

	// Determine the straight-line distance between the centers
	//d = sqrt((dy*dy) + (dx*dx));
	d = hypot(dx, dy);

	/* Check for solvability. */
	if (d > (radius1 + radius2)) {	// no solution: circles do not intersect
		double delta = circle_center1.distance(circle_center2) - radius1 - radius2 + 1e-9; //to avoid 'finite precision' problems
		double coeff1 = radius1 * delta / (radius1 + radius2);
		double coeff2 = radius2 * delta / (radius1 + radius2);
		return circle_intersections(circle_center1, radius1 + coeff1, circle_center2, radius2 + coeff2, intersection1, intersection2);
	}
	if (d < fabs(radius1 - radius2)) {	// no solution: one circle is contained in the other
		double delta, coeff1, coeff2;
		if (radius1 <= 0) {
			intersection1 = circle_center1;
			intersection2 = circle_center1;
			return;
		}
		else if (radius2 <= 0) {
			intersection1 = circle_center2;
			intersection2 = circle_center2;
			return;
		}
		if (radius1 > radius2) {
			delta = radius1 - circle_center1.distance(circle_center2) - radius2 + 1e-9; //to avoid 'finite precision' problems
			coeff1 = radius1 * delta / (radius1 + radius2);
			coeff2 = radius2 * delta / (radius1 + radius2);
			return circle_intersections(circle_center1, radius1 - delta - coeff2, circle_center2, radius2 - coeff2, intersection1, intersection2);
		}
		else {
			delta = radius2 - circle_center1.distance(circle_center2) - radius1 + 1e-9; //to avoid 'finite precision' problems
			coeff1 = radius1 * delta / (radius1 + radius2);
			coeff2 = radius2 * delta / (radius1 + radius2);
			return circle_intersections(circle_center1, radius1 - coeff1, circle_center2, radius2 - delta - coeff1, intersection1, intersection2);
		}
	}

	/* 'point 3' is the point where the line through the circle intersection points crosses the line between the circle centers */

	/* Determine the distance from point 1 to point 3 */
	a = ((radius1*radius1) - (radius2*radius2) + (d*d)) / (2.0 * d);

	// Determine the coordinates of point 3
	x3 = circle_center1.x() + (dx * a / d);
	y3 = circle_center1.y() + (dy * a / d);

	// Determine the distance from point 3 to either of the intersection points/
	h = sqrt((radius1*radius1) - (a*a));

	// Now determine the offsets of the intersection points from point 3
	rx = -dy * (h / d);
	ry = dx * (h / d);

	// Determine the absolute intersection points
	intersection1.update(x3 + rx, y3 + ry);
	intersection2.update(x3 - rx, y3 - ry);

	return;
}


/* Nicolò:
 * determine if the given point is inside or outside the polygon having the boards in its vertices (Jordan theorem)
 */
bool Interpolator::is_inside_perimeter(Coordinates& c) {
	// ~-----local variables------------------------------------------------
	bool out = false;


	// ~-----check solvability----------------------------------------------
	if (_boards_position.size() < 3)
		throw Interpolation_Exception("Not enough vertices to define a polygon");


	// ~-----check if the input parameter is inside or outside the polygon--
	for (int i = 0, j = _boards_position.size() - 1; i < _boards_position.size(); j = i++) {
		if (((_vertices_ordered[i].y() > c.y()) != (_vertices_ordered[j].y() > c.y())) &&
			(c.x() < (_vertices_ordered[j].x() - _vertices_ordered[i].x()) * (c.y() - _vertices_ordered[i].y()) / (_vertices_ordered[j].y() - _vertices_ordered[i].y()) + _vertices_ordered[i].x()))
			out = !out;
	}

	return out;
}


/* Nicolò:
 * determine if the given point is between two points (representing the boards)
 */
bool Interpolator::is_between_points(Coordinates& c) {
	// ~-----local variables------------------------------------------------
	Coordinates p1, p2;


	// ~-----check solvability----------------------------------------------
	if (_boards_position.size() < 2)
		throw Interpolation_Exception("Not enough vertices to define a room");
	else if (_boards_position.size() > 2)
		throw Interpolation_Exception("The actual room has a different shape");


	// ~-----retrieve edges-------------------------------------------------
	p1 = _boards_position.begin()->second;
	p2 = next(_boards_position.begin(), 1)->second;


	// ~-----handle special cases-------------------------------------------
	// vertical points
	if ( p1.x() == p2.x() ) {
		if (c.y() >= min(p1.y(), p2.y()) && c.y() <= max(p1.y(), p2.y()))
			return true;
		else
			return false;
	}
	// horizontal points
	else if ( p1.y() == p2.y() ) {
		if (c.x() >= min(p1.x(), p2.x()) && c.x() <= max(p1.x(), p2.x()))
			return true;
		else
			return false;
	}


	// ~-----check if the input parameter is between two points-------------
	// compute the slope
	double slope = (p2.y() - p1.y()) / (p2.x() - p1.x());
	double perpendicular_slope = -1 / slope;

	// compute the bias
	double offset1		= p1.y() - perpendicular_slope * p1.x();
	double offset2		= p2.y() - perpendicular_slope * p2.x();
	double offset_query	= c.y() - perpendicular_slope * c.x();

	// compare the lines equations (y=mx+q)
	if (offset_query >= min(offset1, offset2) && offset_query <= max(offset1, offset2))
		return true;
	else
		return false;
}



// ~-----operators overloading------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

/* Nicolò:
 * this operator overloading make this class a functional object
 * allowing it to perform some active operations:
 *   - retrieve data from BlockingQueue_Interpolator
 *   - interpolate actual position of the detected smartphone
 *   - calculate timestamp of the original packet
 *   - create a Position object
 *   - fill the database
 */
void Interpolator::operator() () {
	// ~-----local variables------------------------------------------------
	Packet packet;
	Coordinates coordinates;
	chrono::milliseconds timestamp;

	
	// ~-----loop until receives stop signal from external environment------
	while (Synchronizer::_status != Synchronizer::alt) {

		try
		{
			// ~-----retrieve data from BlockingQueue_Interpolator------------------
			packet = _queue_input->retrieve();
			if (packet.empty())	// thread termination
				continue;


			// ~-----compute time and space of the detected smartphone--------------
			try {
				// interpolate actual position
				coordinates = interpolate_position(packet);

				// calculate timestamp
				timestamp = calculate_timestamp(packet);
			}
			catch (Interpolation_Exception& e) {
#if DEBUG
				// console output
				cerr << "Detection discarded due to: " << e.what() << endl;
#endif
				// discard current Position (do NOT insert in database)
				continue;
			}
			catch (exception& e) {
				// notify the occurrence of a problem
				Synchronizer::report_error(Synchronizer::Reason::InterpolatorError,
					"Impossible to interpolate packet from device '" + packet.MACaddress_device() + "' having hash " + to_string(packet.hash()));
			}


			// ~-----create Position object-----------------------------------------
			Position position(timestamp, packet.MACaddress_device(), _configuration_id, coordinates);
			if (packet.is_private()) {
				position.set_private_MACaddress(packet.sequence_number(), packet.SSID(), packet.fingerprint());
			}


			// ~-----insert data in the database------------------------------------
			try {
				position.insertDB();
			}
			catch (Interpolation_Exception& e) {
#if DEBUG
				// console output
				cerr << "Position not inserted in the DB due to: " << e.what() << endl;
#endif
				// discard current Position (do NOT insert in database)
				continue;
			}

		}
		catch (const std::exception& ex)
		{
			stringstream ss;
			ss << "General non-catched exception - " << ex.what();
			Synchronizer::report_error(Synchronizer::InterpolatorError, ss.str());
		}
	}
}