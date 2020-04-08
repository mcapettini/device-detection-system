#pragma once

// ~-----libraries------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
#include "stdafx.h"
//#include "Socket.h"
#include "Aggregator.h"
#include "Interpolator.h"
#include "BlockingQueue_Aggregator.h"
#include "BlockingQueue_Interpolator.h"



// ~-----namespaces-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
using namespace std;



// ~-----types----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

/* Nicolò:
 * type that represent a pointer to a function (represents a delegate)
 * used to perform some action on error occurring
 */
typedef void (__stdcall *PFN_CALLBACK_ERROR) (const char* error_type, const char* error_message);

/* Nicolò:
 * type that represent a pointer to a function (represents a delegate)
 * used to perform some action on automatic setup
 */
typedef void(__stdcall *PFN_CALLBACK_SETUP) (const char* detected_boards);



// ~-----class----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

/* Nicolò:
* software module to start/stop the entire Collector engine (boards + socket + aggregator + interpolator)
* used by C# GUI to communicate with the C++ DLL
*/
class Synchronizer {
public:
	// ~-----enum-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
	enum Reason {
		NoError,
		BadConfiguration,
		DataStructureCollapsed,
		ThreadStopped,
		UnreachableDatabase,
		AggregatorError,
		InterpolatorError,
		SocketError,
		WinsockBadInitialization,
		MemoryError
		//TODO add more
	};

	enum Lead {
		setting_up,
		running,
		alt,
	};



public:
	// ~-----attributes-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
	// error reporting
	static PFN_CALLBACK_ERROR _error_handling_callback;
	static Reason		_reason;
	static string		_message;

	// internal flag
	static Lead				_status/* = alt*/;
	static recursive_mutex	_m;

	// setup information
	static string	_configuration_id;
	static int		_number_boards;

	// engine components
	static unique_ptr<thread>			_socket_ptr;
	static unique_ptr<thread>			_aggregator_ptr;
	static vector<unique_ptr<thread>>	_interpolators_ptr;
	static int							_number_interpolators;

	static shared_ptr<BlockingQueue_Aggregator>		_bq_aggregator_ptr;
	static shared_ptr<BlockingQueue_Interpolator>	_bq_interpolator_ptr;

	// automatic setup
	static PFN_CALLBACK_SETUP		_setup_handling_callback;
	static std::vector<std::string>	_declared_conf;


public:
	// ~-----interface methods------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

	/* Nicolò:
	 * signal to the C# GUI the occurence of an error,
	 * and set the proper variables
	 * C++ -> C#
	 */
	static void report_error(Reason reason, string message);

	/* Nicolò:
	 * signal to the C# GUI the termination of the automatic setup phase,
	 * and pass the detected boards
	 * C++ -> C#
	 */
	static void declare_present_boards(vector<string>& MAC_boards);



//protected:
	// ~-----internal methods-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

	static void parse_string(string input, int number_boards, map<string, Coordinates>& boards_position, vector<Coordinates>& vertices_ordered);

	static void parse_board(string input, map<string, Coordinates>& boards_position, vector<Coordinates>& vertices_ordered);

	static bool equal_configurations(std::vector<std::string>& vec1, std::vector<std::string>& vec2);

	static string enum2string(Reason enumerative);
};



// ~-----DLL exposed functions------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

/* Nicolò:
 * used by the C# GUI to start the entire engine:
 *    - boards sending data
 *    - socket module synchronizing boards and forwarding data
 *    - aggregator module grouping together detection from different boards
 *    - interpolator module computing the position the detected devices
 * C# -> C++
 */
extern "C" __declspec(dllexport) void  __cdecl start_engine(const char* configuration_id, int number_boards, const char* boards_info, PFN_CALLBACK_ERROR error_handling_delegate);

 /* Nicolò:
  * used by the C# GUI to stop the entire engine (due to some problem or setting changes):
  *    - shut down the boards
  *    - join the threads
  *    - empty the data structures queue
  * C# -> C++
  */
extern "C" __declspec(dllexport) void  __cdecl stop_engine(const char* configuration_id);

/* Nicolò:
 * allow the the C# GUI to retrieve the specific of an error (type (enum) and message (string))
 * C++ -> C#
 */
extern "C" __declspec(dllexport) int  __cdecl retrieve_error(const char* configuration_id, char* return_value, int available_len);

/* Nicolò:
 * specify the delegate used to handle a failure in the DLL engine
 * C# -> C++
 */
extern "C" __declspec(dllexport) void  __cdecl error_handling(PFN_CALLBACK_ERROR callback);



/* Nicolò:
 * used by the C# GUI to automatically setup the current configuration (i.e. all the present boards)
 * C# -> C++
 */
extern "C" __declspec(dllexport) void  __cdecl start_setup(PFN_CALLBACK_SETUP setup_handling_delegate, PFN_CALLBACK_ERROR error_handling_delegate);

/* Nicolò:
 * used by the C# GUI to stop the automatic setup
 * C# -> C++
 */
extern "C" __declspec(dllexport) void  __cdecl stop_setup();

/* Nicolò:
 * used by the C# GUI to blink a specific board
 * C# -> C++
 */
extern "C" __declspec(dllexport) void  __cdecl blink(const char* MAC_board);