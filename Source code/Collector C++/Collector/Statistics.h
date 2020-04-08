#pragma once

// ~-----libraries------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
#include "stdafx.h"
#include "Coordinates.h"
#include "Packet.h"



// ~-----class----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

/* Nicolò:
* auxiliry software module to inspect the running environment
* and extract useful statistics
*/
class Statistics {
protected:
	// ~-----constants----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
	static const std::string path_geogebra;
	static const std::string path_performance;



	// ~-----global variables----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

	// general
	static std::map<std::string, Coordinates>	interesting_devices;
	static std::chrono::microseconds			timer_performance;
	static std::chrono::milliseconds			timer_queue;

	static std::mutex	_m_geogebra;
	static std::mutex	_m_performance;
	static std::mutex	_m_queue;


	// RSSI-meter conversion
	static double Ptx;
	static double n;

	// thread performance
	static int	nr_aggregator;
	static int	nr_interpolator;

	// geogebra
	static std::string room_description;


public:
	// ~-----methods----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

	// ~-----general-----
	static void subscribe_device(std::string mac_device, double x, double y);
	static bool is_interesting(Packet& packet);
	static Coordinates real_position(Packet& packet);

	static void start_timer_performance();
	static std::chrono::microseconds current_timer_performance();
	static void start_timer_queue();
	static std::chrono::milliseconds current_timer_queue();


	// ~-----RSSI-meter conversion-----
	static void set_conversion_coefficients(double Ptx, double n);
	static void write_RSSI_vs_distance(Packet& packet, std::map<std::string, Coordinates>& boards_position);
	static void write_position(Packet& packet, Coordinates& detected_position, int nr_boards, std::string algorithm, std::chrono::microseconds time, bool is_inside);



	// ~-----thread performance-----
	static void set_number_threads(int nr_aggregator, int nr_interpolator);
	static void write_aggregator(int queue_size, std::string event);
	static void write_interpolator(int queue_size, std::string event);



	// ~-----geogebra-----
	static void initialize_room(std::vector<Coordinates>& board_position);
	static void create_path();
	static std::string file_path(Packet& packet);

	static void write_room(Packet& packet);
	static void write_circumference(Packet& packet, std::map<std::string, double>& distances);

	static void write_intersections(Packet& packet, std::multimap<std::string, Coordinates>& intersections_all, std::map<std::string, Coordinates>& intersections_filtered);
	static void write_intersections2(Packet& packet, Coordinates& point1, Coordinates& point2);

	static void write_detected(Packet& packet, Coordinates& detected_position);
};


