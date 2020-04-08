// ~-----libraries------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
#include "stdafx.h"
#include "Statistics.h"


// ~-----namespaces-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
using namespace std;



// ~-----fields initialization------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
// constants
const string Statistics::path_geogebra = "..\\output_file\\charts\\geogebra_";
const string Statistics::path_performance = "..\\output_file\\";

// general
map<string, Coordinates>	Statistics::interesting_devices;
chrono::microseconds		Statistics::timer_performance;
chrono::milliseconds		Statistics::timer_queue;

mutex	Statistics::_m_geogebra;
mutex	Statistics::_m_performance;
mutex	Statistics::_m_queue;

// RSSI-meter conversion
double	Statistics::Ptx = -62;
double	Statistics::n	= 2;

// thread performance
int	Statistics::nr_aggregator;
int	Statistics::nr_interpolator;

// geogebra
string	Statistics::room_description;



// ~-----general functions-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

void Statistics::subscribe_device(std::string mac_device, double x, double y) {
	Coordinates c(x, y);
	interesting_devices.insert(make_pair(mac_device, c));
}

bool Statistics::is_interesting(Packet& packet) {
	auto iter = interesting_devices.find(packet.MACaddress_device());

	if (iter == interesting_devices.end())
		return false;
	return true;
}

void Statistics::start_timer_performance() {
	// assign current time
	timer_performance = chrono::duration_cast<chrono::microseconds>(chrono::system_clock::now().time_since_epoch());
}
chrono::microseconds Statistics::current_timer_performance() {
	chrono::microseconds now = chrono::duration_cast<chrono::microseconds>(chrono::system_clock::now().time_since_epoch());
	return now - timer_performance;
}

void Statistics::start_timer_queue() {
	// assign current time
	timer_queue = chrono::duration_cast<chrono::milliseconds>(chrono::system_clock::now().time_since_epoch());
}
chrono::milliseconds Statistics::current_timer_queue() {
	chrono::milliseconds now = chrono::duration_cast<chrono::milliseconds>(chrono::system_clock::now().time_since_epoch());
	return now - timer_queue;
}

Coordinates Statistics::real_position(Packet& packet) {
	auto iter = interesting_devices.find(packet.MACaddress_device());

	if (iter != interesting_devices.end())
		return iter->second;
}



// ~-----RSSI-meter conversion--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

void Statistics::set_conversion_coefficients(double Ptx, double n) {
	Statistics::Ptx = Ptx;
	Statistics::n = n;
}

void Statistics::write_RSSI_vs_distance(Packet& packet, map<string, Coordinates>& boards_position) {
	lock_guard<mutex> lg(_m_performance);

	ifstream test(path_performance + "RSSI_vs_distance.txt");
	bool insert_header = !test.good();

	ofstream performance_ofstream;
	performance_ofstream.open(path_performance + "RSSI_vs_distance.txt", std::ios_base::app);
	if (insert_header)
		performance_ofstream << "timestamp\t" << "MAC_board\t\t" << "RSSI\t" << "real_distance" << endl;

	for (auto iter : packet.RSSIs()) {
		string MAC_board = iter.first;
		double RSSI = iter.second;

		Coordinates board = boards_position.find(MAC_board)->second;
		Coordinates device_real = real_position(packet);

		performance_ofstream << packet.timestamps().begin()->second.count() << "\t";	// print detection timestamp
		performance_ofstream << MAC_board << "\t";										// print board ID
		performance_ofstream << RSSI << "\t";											// print corresponding dB
		performance_ofstream << board.distance(device_real) << endl;					// print real distance
	}
	
	performance_ofstream.close();
}

void Statistics::write_position(Packet& packet, Coordinates& detected_position, int nr_boards, string algorithm, chrono::microseconds time, bool is_inside) {
	lock_guard<mutex> lg(_m_performance);

	ifstream test(path_performance + "detected_position.txt");
	bool insert_header = !test.good();

	ofstream performance_ofstream;
	performance_ofstream.open(path_performance + "detected_position.txt", std::ios_base::app);
	if (insert_header)
		performance_ofstream << "timestamp\t" << "MAC_device\t\t" << "x_real\t" << "y_real\t" << "boards\t" << "algorithm\t" << "µs\t" << "Ptx\t" << "n\t" << "x_detected\t" << "y_detected\t" << "inside_room" << endl;

	performance_ofstream << packet.timestamps().begin()->second.count() << "\t";					// timestamp
	performance_ofstream << packet.MACaddress_device() << "\t";										// device ID
	performance_ofstream << real_position(packet).x() << "\t" << real_position(packet).y() << "\t";	// real position
	performance_ofstream << nr_boards << "\t";														// number boards
	performance_ofstream << algorithm << "\t\t";													// algorithm used to select useful points
	performance_ofstream << time.count() << "\t";													// time performance
	performance_ofstream << Ptx << "\t" << n << "\t";												// conversion parameters
	performance_ofstream << detected_position.x() << "\t\t" << detected_position.y() << "\t";		// detected position
	performance_ofstream << to_string(is_inside) << endl;											// inside-outside the room

	performance_ofstream.close();
}



// ~-----thread performance--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
void Statistics::set_number_threads(int nr_aggregator, int nr_interpolator) {
	Statistics::nr_aggregator = nr_aggregator;
	Statistics::nr_interpolator = nr_interpolator;
}

void Statistics::write_aggregator(int queue_size, string event) {
	lock_guard<mutex> lg(_m_queue);

	ifstream test(path_performance + "aggregator_" + to_string(nr_aggregator) + "thread.txt");
	bool insert_header = !test.good();

	ofstream queue_ofstream;
	queue_ofstream.open(path_performance + "aggregator_" + to_string(nr_aggregator) + "thread.txt", std::ios_base::app);
	if (insert_header)
		queue_ofstream << "epoch\t" << "time\t" << "size\t" << "event" << endl;


	queue_ofstream << chrono::duration_cast<chrono::milliseconds>(chrono::system_clock::now().time_since_epoch()).count() << "\t";
	queue_ofstream << current_timer_queue().count() << "\t";	// print notification timestamp
	queue_ofstream << queue_size << "\t";						// print number of elements waiting in the queue
	queue_ofstream << event << endl;							// print optional events

	queue_ofstream.close();
}

void Statistics::write_interpolator(int queue_size, string event) {
	lock_guard<mutex> lg(_m_queue);

	ifstream test(path_performance + "interpolator_" + to_string(nr_interpolator) + "thread.txt");
	bool insert_header = !test.good();

	ofstream queue_ofstream;
	queue_ofstream.open(path_performance + "interpolator_" + to_string(nr_interpolator) + "thread.txt", std::ios_base::app);
	if (insert_header)
		queue_ofstream << "epoch\t" << "time\t" << "size\t" << "event" << endl;


	queue_ofstream << chrono::duration_cast<chrono::milliseconds>(chrono::system_clock::now().time_since_epoch()).count() << "\t";
	queue_ofstream << current_timer_queue().count() << "\t";	// print notification timestamp
	queue_ofstream << queue_size << "\t";						// print number of elements waiting in the queue
	queue_ofstream << event << endl;							// print optional events

	queue_ofstream.close();
}



// ~-----geogebra--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

void Statistics::initialize_room(vector<Coordinates>& board_position) {
	// initialize strings
	room_description = "";
	string polygon = "q1=Poligono(";

	// iterate over the board
	for (int i = 0; i < board_position.size(); i++) {
		room_description += "V" + to_string(i + 1) + "=(" + to_string(board_position[i].x()) + "," + to_string(board_position[i].y()) + ")\nImpColore(" + "V" + to_string(i + 1) + ",\"Black\")\n";
		polygon += "V" + to_string(i + 1) + ",";
	}

	// fix last details
	polygon = polygon.substr(0, polygon.length() - 1);
	polygon += ")\nImpColore(q1,\"Black\")\n";
	room_description += polygon;
}

void Statistics::create_path() {
	system("mkdir ..\\output_file\\charts");
}

string Statistics::file_path(Packet& packet) {
	string mac = packet.MACaddress_device();
	string timestamp = to_string(packet.timestamps().begin()->second.count());

	std::stringstream ss;
	std::replace(mac.begin(), mac.end(), ':', '-');
	ss << path_geogebra << mac << "_" << timestamp << ".txt";

	return ss.str();
}

void Statistics::write_room(Packet& packet) {
	lock_guard<mutex> lg(_m_geogebra);

	ofstream geogebra_ofstream;
	geogebra_ofstream.open(file_path(packet), std::ios_base::app);

	geogebra_ofstream << room_description;

	geogebra_ofstream.close();
}

void Statistics::write_circumference(Packet& packet, map<string, double>& distances) {
	lock_guard<mutex> lg(_m_geogebra);
	int index = 1;

	ofstream geogebra_ofstream;
	geogebra_ofstream.open(file_path(packet), std::ios_base::app);

	for (auto iterator : distances) {
		geogebra_ofstream << "c" << index << ": Circonferenza(V" << index << ", " << iterator.second << ")\n";
		geogebra_ofstream << "ImpColore(c" << index << ", \"Light Gray\")\n";

		index++;
	}

	geogebra_ofstream.close();
}

void Statistics::write_intersections(Packet& packet, multimap<string, Coordinates>& intersections_all, map<string, Coordinates>& intersections_filtered) {
	lock_guard<mutex> lg(_m_geogebra);
	int index = 1;

	ofstream geogebra_ofstream;
	geogebra_ofstream.open(file_path(packet), std::ios_base::app);
	geogebra_ofstream << uppercase; //compatibility with Geogebra 'E' scientific notation

	for (auto iter = intersections_all.begin(); iter != intersections_all.end(); iter++, index++) {
		multimap<string, Coordinates>::iterator discarded, kept;

		// distinguish between kept and discarded
		if (intersections_filtered.find(iter->first) != intersections_filtered.end()) {	//iter is kept
			kept = iter;
			discarded = ++iter;
		}
		else {	//iter is discarded
			discarded = iter;
			kept = ++iter;
		}

		// print the discarded points
		geogebra_ofstream << "D" << index << "=(" << discarded->second.x() << ", " << discarded->second.y() << ")\n";
		geogebra_ofstream << "ImpColore(D" << index << ", \"Orange\")\n";

		// print the kept points
		geogebra_ofstream << "K" << index << "=(" << kept->second.x() << ", " << kept->second.y() << ")\n";
		geogebra_ofstream << "ImpColore(K" << index << ", \"Green\")\n";
	}

	geogebra_ofstream.close();
}

void Statistics::write_intersections2(Packet& packet, Coordinates& point1, Coordinates& point2) {
	lock_guard<mutex> lg(_m_geogebra);

	ofstream geogebra_ofstream;
	geogebra_ofstream.open(file_path(packet), std::ios_base::app);
	geogebra_ofstream << uppercase; //compatibility with Geogebra 'E' scientific notation

	geogebra_ofstream << "K1=(" + to_string(point1.x()) + ", " + to_string(point1.y()) + ")\n";
	geogebra_ofstream << "ImpColore(K1, \"Green\")\n";
	geogebra_ofstream << "K2=(" + to_string(point2.x()) + ", " + to_string(point2.y()) + ")\n";
	geogebra_ofstream << "ImpColore(K2, \"Green\")\n";

	geogebra_ofstream.close();
}

void Statistics::write_detected(Packet& packet, Coordinates& detected_position) {
	lock_guard<mutex> lg(_m_geogebra);

	ofstream geogebra_ofstream;
	geogebra_ofstream.open(file_path(packet), std::ios_base::app);
	geogebra_ofstream << uppercase; //compatibility with Geogebra 'E' scientific notation

	geogebra_ofstream << "DETECTED" << "=(" << to_string(detected_position.x()) << "," << to_string(detected_position.y()) << ")\n";
	geogebra_ofstream << "ImpColore(DETECTED, \"Red\")\n";
	if (is_interesting(packet)) {
		geogebra_ofstream << "REAL" << "=(" << to_string(real_position(packet).x()) << "," << to_string(real_position(packet).y()) << ")\n";
		geogebra_ofstream << "ImpColore(REAL, \"Red\")\n";
		geogebra_ofstream << "d=Distanza(REAL, DETECTED)\ntesto=\"d=\"+d\n";
	}

	geogebra_ofstream.close();
}