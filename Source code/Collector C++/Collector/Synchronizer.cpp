// ~-----libraries------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
#include "stdafx.h"
#include "Synchronizer.h"
#include "SocketStub.h"
#include "Socket.h"



// ~-----namespaces-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
using namespace std;



// ~-----fields initialization------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
// error reporting
PFN_CALLBACK_ERROR			Synchronizer::_error_handling_callback	= nullptr;
Synchronizer::Reason	Synchronizer::_reason					= NoError;
string					Synchronizer::_message					= "";

// internal flag
Synchronizer::Lead	Synchronizer::_status = alt;
recursive_mutex		Synchronizer::_m;

// setup information
string	Synchronizer::_configuration_id	= "";
int		Synchronizer::_number_boards	= -1;

// engine components
unique_ptr<thread>			Synchronizer::_socket_ptr			= nullptr;
unique_ptr<thread>			Synchronizer::_aggregator_ptr		= nullptr;
vector<unique_ptr<thread>>	Synchronizer::_interpolators_ptr;
int							Synchronizer::_number_interpolators	= -1;

shared_ptr<BlockingQueue_Aggregator>	Synchronizer::_bq_aggregator_ptr;
shared_ptr<BlockingQueue_Interpolator>	Synchronizer::_bq_interpolator_ptr;

// automatic setup
PFN_CALLBACK_SETUP	Synchronizer::_setup_handling_callback;
vector<string> Synchronizer::_declared_conf;

// internal facilities
unique_ptr<Socket>	_socket_functor_ptr	= nullptr;



// ~-----DLL exposed functions------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

/* Nicolò:
 * used by the C# GUI to start the entire engine:
 *    - boards sending data
 *    - socket module synchronizing boards and forwarding data
 *    - aggregator module grouping together detection from different boards
 *    - interpolator module computing the position the detected devices
 * C# -> C++
 */
extern "C" __declspec(dllexport) void  __cdecl start_engine(const char* configuration_id, int number_boards, const char* boards_info, PFN_CALLBACK_ERROR error_handling_delegate) {
	// ~-----local variables------------------------------------------------
	lock_guard<recursive_mutex> lg(Synchronizer::_m);
	map<string, Coordinates>	boards_position;
	vector<Coordinates>			vertices_ordered;
	vector<string>				boards_mac;

	
	// ~-----update current status------------------------------------------
	Synchronizer::_status = Synchronizer::setting_up;


	// ~-----handle input parameters----------------------------------------
	//error handling function
	if (error_handling_delegate == nullptr)
		throw invalid_argument("The delegate to handle errors is not defined");
	Synchronizer::_error_handling_callback = error_handling_delegate;


	// configuration name
	string tempID(configuration_id);
	if (tempID.empty())
		Synchronizer::report_error(Synchronizer::BadConfiguration, "Is not specified the configuration name");
	Synchronizer::_configuration_id = tempID;

	// number of boards
	if (number_boards < 2)
		Synchronizer::report_error(Synchronizer::BadConfiguration, "The boards should be at least 2");
	Synchronizer::_number_boards = number_boards;
	Synchronizer::_number_interpolators = 1;

	// boards MAC and Coordinates
	Synchronizer::parse_string(boards_info, number_boards, boards_position, vertices_ordered);
	if (number_boards != vertices_ordered.size())
		Synchronizer::report_error(Synchronizer::BadConfiguration, "The declared number of boards differ from the configuration string");
	for (auto iter = boards_position.begin(); iter != boards_position.end(); iter++)
		boards_mac.push_back(iter->first);
#if DEBUG
	Statistics::create_path();
	Statistics::initialize_room(vertices_ordered);
#endif


	// ~-----check duplicated calls-----------------------------------------
	if (tempID == Synchronizer::_configuration_id && Synchronizer::_status == Synchronizer::running)
		return;


	// ~-----create synchronization data structures-------------------------
	Synchronizer::_bq_aggregator_ptr	= make_shared<BlockingQueue_Aggregator>(Synchronizer::_number_boards);
	Synchronizer::_bq_interpolator_ptr	= make_shared<BlockingQueue_Interpolator>(Synchronizer::_number_boards);
#if DEBUG
	Statistics::start_timer_queue();
	Statistics::set_number_threads(1, Synchronizer::_number_interpolators);
#endif


	// ~-----launch the threads---------------------------------------------
	// launch socket
	if (_socket_functor_ptr == nullptr) {	//there was NOT auto-setup
		// start new socket
		_socket_functor_ptr			= make_unique<Socket>(Synchronizer::_number_boards, boards_mac, Synchronizer::_bq_aggregator_ptr);
		Synchronizer::_socket_ptr	= make_unique<thread>(*_socket_functor_ptr);
	}
	else {	// there was auto-setup
		if ( ! Synchronizer::equal_configurations(Synchronizer::_declared_conf, boards_mac) ) {
			// close previous socket
			_socket_functor_ptr->stopServer();
			if (Synchronizer::_socket_ptr != nullptr) {
				Synchronizer::_socket_ptr->join();
			}
			// start new socket
			Synchronizer::_status		= Synchronizer::setting_up;
			_socket_functor_ptr			= make_unique<Socket>(Synchronizer::_number_boards, boards_mac, Synchronizer::_bq_aggregator_ptr);
			Synchronizer::_socket_ptr	= make_unique<thread>(*_socket_functor_ptr);
		}
		else {
			// re-use the same sockets (already opened for auto-setup)
			_socket_functor_ptr->concludeAutoSetup(Synchronizer::_number_boards, boards_mac, Synchronizer::_bq_aggregator_ptr);
		}
	}

	// launch aggregator
	Aggregator agg(Synchronizer::_number_boards, boards_mac, Synchronizer::_bq_aggregator_ptr, Synchronizer::_bq_interpolator_ptr);
	Synchronizer::_aggregator_ptr = make_unique<thread>(agg);

	// launch interpolators
	for (int i = 0; i < Synchronizer::_number_interpolators; i++) {
		Interpolator interp(Synchronizer::_configuration_id, boards_position, vertices_ordered, Synchronizer::_bq_interpolator_ptr);
		auto ptr_temp = make_unique<thread>(interp);
		Synchronizer::_interpolators_ptr.push_back(move(ptr_temp));
	}


	// ~-----update current status------------------------------------------
	Synchronizer::_status = Synchronizer::running;
}


/* Nicolò:
 * used by the C# GUI to stop the entire engine (due to some problem or setting changes):
 *    - shut down the boards
 *    - join the threads
 *    - empty the data structures queue
 * C# -> C++
 */
extern "C" __declspec(dllexport) void  __cdecl stop_engine(const char* configuration_id) {
	// ~-----local variables------------------------------------------------
	lock_guard<recursive_mutex> lg(Synchronizer::_m);


	// ~-----check if is the desired engine----------------------------------
	string conf(configuration_id);
	if (!Synchronizer::_configuration_id.empty() && conf != Synchronizer::_configuration_id)
		return;


	// ~-----check duplicated calls-----------------------------------------
	if (!Synchronizer::_configuration_id.empty() && conf == Synchronizer::_configuration_id
			&& Synchronizer::_status == Synchronizer::alt)
		return;


	// ~-----update current status------------------------------------------
	Synchronizer::_status = Synchronizer::alt;


	// ~-----join the threads-----------------------------------------------
	// join socket
	if (_socket_functor_ptr != nullptr) {
		_socket_functor_ptr->stopServer();
	}
	if (Synchronizer::_socket_ptr != nullptr) {
		Synchronizer::_socket_ptr->join();
	}

	// join aggregator
	if (Synchronizer::_aggregator_ptr != nullptr) {
		Synchronizer::_bq_aggregator_ptr->notify_threads();
		Synchronizer::_aggregator_ptr->join();
	}

	// join interpolators
	if (!Synchronizer::_interpolators_ptr.empty()) {
		Synchronizer::_bq_interpolator_ptr->notify_threads();
		for (int i = 0; i < Synchronizer::_number_interpolators; i++) {
			Synchronizer::_interpolators_ptr[i]->join();
		}
	}


	// ~-----empty the data structures--------------------------------------
	// clear the blocking queue
	if (Synchronizer::_bq_aggregator_ptr != nullptr) {
		Synchronizer::_bq_aggregator_ptr->clear();
	}
	if (Synchronizer::_bq_interpolator_ptr != nullptr) {
		Synchronizer::_bq_interpolator_ptr->clear();
	}
	// clear the auto-setup proposed configuration
	Synchronizer::_declared_conf.clear();


	// ~-----release the resources------------------------------------------
	// free thread pointers
	_socket_functor_ptr				= nullptr;
	Synchronizer::_socket_ptr		= nullptr;
	Synchronizer::_aggregator_ptr	= nullptr;
	for (int i = (int)Synchronizer::_interpolators_ptr.size() - 1; i >= 0; i--) {
		Synchronizer::_interpolators_ptr[i] = nullptr;
		Synchronizer::_interpolators_ptr.pop_back();
	}

	// free data structures
	Synchronizer::_bq_aggregator_ptr	= nullptr;
	Synchronizer::_bq_interpolator_ptr	= nullptr;
}


/* Nicolò:
 * allow the the C# GUI to retrieve the specific of an error (type (enum) and message (string))
 * C++ -> C#
 */
extern "C" __declspec(dllexport) int  __cdecl retrieve_error(const char* configuration_id, char* return_value, int available_len) {
	// ~-----local variables------------------------------------------------
	lock_guard<recursive_mutex> lg(Synchronizer::_m);
	string out;


	// ~-----check input parameters-----------------------------------------
	if (configuration_id == nullptr || return_value == nullptr || available_len < 1)
		return -1;


	// ~-----check if is the desired configuration--------------------------
	string conf(configuration_id);
	if (!Synchronizer::_configuration_id.empty() && conf != Synchronizer::_configuration_id)
		return -1;


	// ~-----check occurence of errors--------------------------------------
	if (Synchronizer::_reason == Synchronizer::NoError)
		return -1;


	// ~-----provide information--------------------------------------------
	// parse error type and related message
	out = Synchronizer::enum2string(Synchronizer::_reason) + "_" + Synchronizer::_message;

	// discard overflow chars
	if (out.length() > available_len)
		out = out.substr(0, available_len);

	// copy to the 'by reference' parameter
	copy(out.begin(), out.end(), return_value);

	// return the number of used chars
	return (int)strlen(return_value);
}


/* Nicolò:
 * specify the delegate used tp handle a failure in the DLL engine
 * C# -> C++
 */
extern "C" __declspec(dllexport) void  __cdecl error_handling(PFN_CALLBACK_ERROR callback) {
	// protect the access
	lock_guard<recursive_mutex> lg(Synchronizer::_m);

	// update Synchronizer information
	Synchronizer::_error_handling_callback = callback;
}


/* Nicolò:
 * used by the C# GUI to automatically setup the current configuration (i.e. all the present boards)
 * C# -> C++
 */
extern "C" __declspec(dllexport) void  __cdecl start_setup(PFN_CALLBACK_SETUP setup_handling_delegate, PFN_CALLBACK_ERROR error_handling_delegate) {

	// ~-----handle input parameters----------------------------------------
	// setup handling function
	if (setup_handling_delegate == nullptr)
		throw invalid_argument("The delegate to handle automatic setup is not defined");
	Synchronizer::_setup_handling_callback = setup_handling_delegate;
	// error handling function
	if (error_handling_delegate == nullptr)
		throw invalid_argument("The delegate to handle errors is not defined");
	Synchronizer::_error_handling_callback = error_handling_delegate;


	// ~-----update current status------------------------------------------
	Synchronizer::_status = Synchronizer::setting_up;


	// ~-----launch the threads---------------------------------------------
	// launch socket
	_socket_functor_ptr			= make_unique<Socket>();
	Synchronizer::_socket_ptr	= make_unique<thread>(*_socket_functor_ptr);
}


/* Nicolò:
 * used by the C# GUI to stop the automatic setup
 * C# -> C++
 */
extern "C" __declspec(dllexport) void  __cdecl stop_setup() {

	// ~-----update current status------------------------------------------
	Synchronizer::_status = Synchronizer::alt;

	// ~-----join the threads-----------------------------------------------
	// join socket
	if (_socket_functor_ptr != nullptr) {
		_socket_functor_ptr->stopServer();
	}
	if (Synchronizer::_socket_ptr != nullptr) {
		Synchronizer::_socket_ptr->join();
	}

	// ~-----release the resources------------------------------------------
	// free thread pointers
	_socket_functor_ptr = nullptr;
	Synchronizer::_socket_ptr = nullptr;

	// ~-----empty the data structures--------------------------------------
	// clear the auto-setup proposed configuration
	Synchronizer::_declared_conf.clear();
}


/* Nicolò:
 * used by the C# GUI to blink a specific board
 * C# -> C++
 */
extern "C" __declspec(dllexport) void  __cdecl blink(const char* MAC_board) {
	// ~-----local variables------------------------------------------------
	string board(MAC_board);

	// ~-----check current status-------------------------------------------
	if (_socket_functor_ptr == nullptr || Synchronizer::_status != Synchronizer::setting_up)
		throw invalid_argument("The setup phase is not active");

	// ~-----actually blink the selected board------------------------------
	_socket_functor_ptr->Blink(board);
}



// ~-----interface methods----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

/* Nicolò:
 * signal to the C# GUI the occurence of an error,
 * and set the proper variables
 * C++ -> C#
 */
void Synchronizer::report_error(Reason reason, string message) {
	// ~-----local variables------------------------------------------------
	lock_guard<recursive_mutex> lg(_m);


	// ~-----set variables--------------------------------------------------
	_reason = reason;
	_message = message;


	// ~-----console output-------------------------------------------------
#if DEBUG
	cerr << "Error occourred in the engine:\n   " << enum2string(_reason) << ": " << _message << flush << endl;
#endif


	// ~-----invoke C# delegate for error handling--------------------------
	if (_error_handling_callback != nullptr) {
		_error_handling_callback(enum2string(reason).c_str(), message.c_str());
	}
}

/* Nicolò:
 * signal to the C# GUI the termination of the automatic setup phase,
 * and pass the detected boards
 * C++ -> C#
 */
void Synchronizer::declare_present_boards(vector<string>& MAC_boards) {
	// ~-----local variables------------------------------------------------
	string output;

	// ~-----check input parameters-----------------------------------------
	if (MAC_boards.size() < 1)
		return;

	// ~-----store the proposed configuration-------------------------------
	_declared_conf.clear();
	for (string item : MAC_boards) {
		_declared_conf.push_back(item);
	}

	// ~-----serialize MACs-------------------------------------------------
	for (string s : MAC_boards)
		output.append(s + "_");
	output = output.substr(0, output.length() - 1);

	// ~-----notify the C# invoking the callback----------------------------
	if (_setup_handling_callback != nullptr)
		_setup_handling_callback(output.c_str());
}



// ~-----internal methods-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

void Synchronizer::parse_string(string s, int number_boards, map<string, Coordinates>& boards_position, vector<Coordinates>& vertices_ordered) {
	// ~-----local variables------------------------------------------------
	string input(s);
	std::string delimiter = "_";
	size_t pos = 0;
	std::string token;
	int i;


	// ~-----split string into boards---------------------------------------
	// iterate for N-1 boards
	for (i = 0; i < number_boards && pos != std::string::npos; i++) {
		pos = input.find(delimiter);
		token = input.substr(0, pos);
		input.erase(0, pos + delimiter.length());

		parse_board(token, boards_position, vertices_ordered);
	}

	// check configuration errors
	if (i < number_boards || input.find(delimiter) != std::string::npos)
		report_error(BadConfiguration, "The number of given boards differs from the declared one");

	// handle the last board
	//parse_board(input, boards_position, vertices_ordered);
}


void Synchronizer::parse_board(string input, map<string, Coordinates>& boards_position, vector<Coordinates>& vertices_ordered) {
	// ~-----local variables------------------------------------------------
	size_t pos = 0;
	std::string delimiter = "|";


	// ~-----split string into fields---------------------------------------
	// retrieve MAC
	pos = input.find(delimiter);
	std::string mac = input.substr(0, pos);
	input.erase(0, pos + delimiter.length());

	// retrieve X
	pos = input.find(delimiter);
	std::string x_str = input.substr(0, pos);
	double x = strtod(x_str.c_str(), nullptr);
	input.erase(0, pos + delimiter.length());

	// retrieve Y
	std::string y_str = input;
	double y = strtod(y_str.c_str(), nullptr);
	input.erase(0, y_str.length() + delimiter.length());


	// ~-----store data-----------------------------------------------------
	// create object
	Coordinates c(x, y);

	// insert in the collection
	boards_position.insert(make_pair(mac, c));
	vertices_ordered.push_back(c);
}

bool Synchronizer::equal_configurations(vector<string>& vec1, vector<string>& vec2) {
	// check vectors dimension
	if (vec1.size() != vec2.size())
		return false;

	// check if both vectors contain the same elements
	for (string item : vec1) {
		if (find(vec2.begin(), vec2.end(), item) == vec2.end()) { // not found
			return false;
		}
	}
	return true;
}

string Synchronizer::enum2string(Synchronizer::Reason enumerative) {
	switch (enumerative) {
	case NoError:
		return "NoError";
	case BadConfiguration:
		return "BadConfiguration";
	case DataStructureCollapsed:
		return "DataStructureCollapsed";
	case ThreadStopped:
		return "ThreadStopped";
	case UnreachableDatabase:
		return "UnreachableDatabase";
	case AggregatorError:
		return "AggregatorError";
	case InterpolatorError:
		return "InterpolatorError";
	case SocketError:
		return "SocketError";
	case WinsockBadInitialization:
		return "WinsockBadInitialization";
	case MemoryError:
		return "MemoryError";
	//TODO add more
	}
}