#pragma once

// ~-----libraries------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
#include "stdafx.h"
#include "Detection.h"
#include "BlockingQueue_Aggregator.h"



// ~-----namespaces-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
using namespace std;



// ~-----class----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

/* Nicolò:
 * functional object that emulate the behaviour of the actual Socket software module
 * by inserting data into BlockingQueue_Aggregator, in Detection object format
 */
class SocketStub
{
protected:
	// ~-----attributes-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
	int										_number_boards;
	shared_ptr<BlockingQueue_Aggregator>	_bq_aggregator_ptr;
	map<string, Coordinates>				_boards_position;
	//int										_number_threads = 4;



public:
	// ~-----constructors and destructors-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
	SocketStub(int number_boards, shared_ptr<BlockingQueue_Aggregator> bq_ptr, map<string, Coordinates> boards_position);



	// ~-----methods----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

	static string randMAC();

	static map<string, Coordinates> randBoards(int number_boards);

	static string randBoards_toConfString(int number_boards);



	// ~-----operators overloading------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

	/* Nicolò:
	 * this operator overloading make this class a functional object
	 * allowing it to perform some active operations:
	 *   - generate several threads
	 *   - generate a probable position fo a detected device (according to the boards position)
	 *   - introducing an error in the measurements
	 *   - inserting data into BlockingQueue_Aggregator
	 */
	void operator() ();
};

