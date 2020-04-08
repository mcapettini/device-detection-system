#pragma once

// ~-----libraries------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
#include "stdafx.h"
#include "Position.h"
#include "Packet.h"
#include "BlockingQueue_Interpolator.h"
#include "Statistics.h"
#include "Interpolation_Exception.h"



// ~-----class----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

/* Nicolò:
 * functional object that request data from BlockingQueue_Aggregator, in Detection object format,
 * aggregate them in a Packet object, and fill the BlockingQueue_Interpolator
 */
class Interpolator
{
public:
	// ~-----enum-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
	enum AlgorithmType {
		fast_unordered,
		fast_ordered,
		hybrid_unordered,
		hybrid_ordered,
		accurate_unordered,
		accurate_ordered
	};

	enum ParametersType {
		literature,
		heuristic,
		regression
	};


protected:
	// ~-----attributes-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
	std::string									_configuration_id;
	std::map<std::string, Coordinates>			_boards_position;
	std::vector<Coordinates>					_vertices_ordered;
	std::shared_ptr<BlockingQueue_Interpolator>	_queue_input;
	AlgorithmType								_algorithm = fast_unordered;
	ParametersType								_parameters = regression;


public:
	// ~-----constructors and destructors-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
	Interpolator(std::string configuration_id, std::map<std::string, Coordinates> boards_position, std::vector<Coordinates> ordered_boards, std::shared_ptr<BlockingQueue_Interpolator> queue_input_ptr);



	// ~-----methods----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

	/* Nicolò:
	 * interpolate the position of the detected smartphone,
	 * given the list of the couples:
	 *   - Coordinates (x:y) of a board
	 *   - RSSI (signal power) of a board listening
	 */
	Coordinates interpolate_position(Packet& packet);


	/* Nicolò:
	 * streaming clustering algorithm to determine the interesting intersection in each pair
	 * lighter (less computations) but faster (more performance) version, with the following features:
	 *    - unordered: does not sort the incoming intersections
	 *    - without elimination: keeps the intersections discarded in the previous iterations
	 */
	std::map<std::string, Coordinates> clusterize_fast(std::vector<std::string> couple_IDs, std::multimap<std::string, Coordinates> intersections_all);

	/* Nicolò:
	 * streaming clustering algorithm to determine the interesting intersection in each pair
	 * intermediate version, with the following features:
	 *    - unordered: does not sort the incoming intersections
	 *    - with elimination: at each iteration discard the not useful intersection
	 */
	std::map<std::string, Coordinates> clusterize_hybrid(std::vector<std::string> couple_IDs, std::multimap<std::string, Coordinates> intersections_all);

	/* Nicolò:
	 * streaming clustering algorithm to determine the interesting intersection in each pair
	 * heavier (more computations) but more precise (less error) version, with the following features:
	 *    - ordered: sort the incoming intersections according to the radius of the circumferences from which they are calculated
	 *    - with elimination: at each iteration discard the not useful intersection
	 */
	std::map<std::string, Coordinates> clusterize_accurate(std::vector<std::string> couple_IDs, std::multimap<std::string, Coordinates> intersections_all, std::map<std::string, double> distances);

	/* Nicolò:
	 * calculate the timestamp at which the WiFi packet was originally sent,
	 * given the list of the couples:
	 *   - Coordinates (x:y) of a board
	 *   - timestamp of a board listening
	 */
	std::chrono::milliseconds calculate_timestamp(Packet& packet);

	/* Nicolò:
	 * convert the power RSSI of a WiFi signal (received by a board),
	 * in its corresponding distance (in meters)
	 */
	double convert_RSSI_to_meter(double RSSI);

	/* Nicolò:
	 * calculate the two coordinates of the intersection of two
	 * bidimensional circumferences
	 */
	void circle_intersections(Coordinates circle_center1, double radius1, Coordinates circle_center2, double radius2, Coordinates& intersection1, Coordinates& intersection2);

	/* Nicolò:
	 * determine if the given point is inside or outside the polygon having the boards in its vertices
	 */
	bool is_inside_perimeter(Coordinates& c);

	/* Nicolò:
	 * determine if the given point is between two points (representing the boards)
	 */
	bool is_between_points(Coordinates& c);


	// ~-----operators overloading--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

	/* Nicolò:
	 * this operator overloading make this class a functional object
	 * allowing it to perform some active operations:
	 *   - retrieve data from BlockingQueue_Interpolator
	 *   - interpolate actual position of the detected smartphone
	 *   - calculate timestamp of the original packet
	 *   - create a Position object
	 *   - fill the database
	 */
	void operator() ();
};

