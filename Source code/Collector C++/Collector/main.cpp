// Collector.cpp: definisce il punto di ingresso dell'applicazione console.
//


// ~-----libraries------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
#include "stdafx.h"
#include "Coordinates.h"
#include "Position.h"
#include "Detection.h"
#include "Packet.h"
#include "Aggregator.h"
#include "Interpolator.h"
#include "BlockingQueue_Aggregator.h"
#include "BlockingQueue_Interpolator.h"
#include "Synchronizer.h"
#include "SocketStub.h"
#include "Socket.h"
#include "Synchronizer.h"
#include "Statistics.h"


// ~-----constants------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------



// ~-----namespace------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
using namespace std;


// ~-----global variables-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
string configuration_id;
int nr_board;

mutex m_errorstop;
condition_variable cv_errorstop;
unique_ptr<thread> thread_errorstop;


// ~-----prototypes-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
string build_configuration_string(vector<string> board_mac, vector<Coordinates> board_position);
void wait_then_stop();
void error_handler(const char* error_type, const char* error_message);
void autosetup_handler(const char* detected_boards);


// ~-----starting point-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

/* Nicolò:
 * It is the starting point of the C++ application.
 * It takes care of acquiring the command line parameter:
 *	- number of devices
 *	- coordinates of those devices
 * and also to launching the needed threads.
 */
int main(int argc, char *argv[]) {
	// ~-----local variables------------------------------------------------
	vector<Coordinates> board_position;
	vector<string> board_mac;
	string conf_string;	//format: <mac1>|<x1>|<y1>_<mac2>|<x2>|<y2>_...


	// ~-----define the 'configuration' settings----------------------------
	configuration_id = "stanza Nico";
	nr_board = 3;


	// optA: socket stub (emulated boards)
	conf_string = SocketStub::randBoards_toConfString(nr_board);


	// optB: actual socket (real boards)
	string board_nico = "30:ae:a4:3b:a2:d8";
	string board_fabio = "30:ae:a4:1b:c8:58";
	string board_cape = "24:0a:c4:0a:f1:7c";
	string board_matte = "24:0a:c4:0a:6f:1c";

	board_mac.push_back(board_cape);	board_position.push_back(Coordinates(2.068, 1.45));
	board_mac.push_back(board_nico);	board_position.push_back(Coordinates(-0.892, 0.76));
	board_mac.push_back(board_matte);	board_position.push_back(Coordinates(0.505, -2.73));

	conf_string = build_configuration_string(board_mac, board_position);

#if DEBUG
	Statistics::initialize_room(board_position);
#endif


	// ~-----define which device we desire to detect------------------------
#if DEBUG
	//Statistics::subscribe_device("94:65:2d:2f:51:58", 0, 1.4);
#endif


	// ~-----auxiliary thread to emulate C# thread pool--------------------
	// start the auxiliry thread to stop engine
	thread_errorstop = make_unique<thread>(wait_then_stop);


	// ~-----auto-setup-----------------------------------------------------
	// start the self discovery
	start_setup(autosetup_handler, error_handler);

	// blink the first board
	blink("");


	// ~-----handle output files--------------------------------------------
	// clear all the output directories
	//system("del /Q .\\output_file\\charts\\*.txt");
	//system("del /Q .\\output_file\\*.txt");


	// ~-----actually act on the engine-------------------------------------
	// start engine
	start_engine(configuration_id.c_str(), nr_board, conf_string.c_str(), error_handler);

	// emulate GUI doing something else
	this_thread::sleep_for(chrono::minutes(5));
	cout << "The emulated C# GUI requested to stop the engine!" << endl;

	// stop engine
	stop_engine(configuration_id.c_str());


	// ~-----auxiliary thread to emulate C# thread pool--------------------
	// join the terminator
	thread_errorstop->join();
	thread_errorstop = nullptr;


    return 0;
}



// ~-----functions------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

string build_configuration_string(vector<string> board_mac, vector<Coordinates> board_position) {
	// local variables
	string output;

	// iterate over the board
	for (int i = 0, index = 1; i < board_mac.size(); i++, index++) {
		output += board_mac[i] + "|" + to_string(board_position[i].x()) + "|" + to_string(board_position[i].y()) + "_";
	}

	// fix last details
	output = output.substr(0, output.length() - 1);

	// return the configuration_string
	return output;
}

void wait_then_stop() {
	// wait until an error is rised
	unique_lock<mutex> ul(m_errorstop);
	cv_errorstop.wait(ul);

	// stop engine
	stop_engine(configuration_id.c_str());
}


void error_handler(const char* error_type, const char* error_message) {
	// notify the occurrence of an error
	cerr << endl << "---------- error handler ----------" << endl;
	cerr << "An error accurred:" << endl;
	cerr << "   " << error_type << ": " << error_message << endl;
	cerr << "The preposed terminating thread will be woke up" << endl;

	// activate the thread preposed to stop
	cv_errorstop.notify_all();
}


void autosetup_handler(const char* detected_boards) {
	// notify the occurrence of an error
	cerr << endl << "---------- auto-setup handler ----------" << endl;
	cerr << "The following boards were discovered:" << endl;
	cerr << "   " << detected_boards << endl;
}

