#pragma once

// ~-----libraries------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
#include "stdafx.h"
#include "Detection.h"
#include "BlockingQueue_Aggregator.h"
#include "Packet.h"
#include "BlockingQueue_Interpolator.h"



// ~-----class----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

/* Nicolò:
 * functional object that request data from BlockingQueue_Aggregator, in Detection object format,
 * aggregate them in a Packet object, and fill the BlockingQueue_Interpolator
 */
class Aggregator
{
protected:
	// ~-----attributes-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
	int											_number_boards;
	std::vector<std::string>							_boards_mac;
	std::shared_ptr<BlockingQueue_Aggregator>	_queue_input;
	std::shared_ptr<BlockingQueue_Interpolator>	_queue_output;

public:
	// ~-----constructors and destructors-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
	Aggregator(int number_boards,
		std::vector<std::string> boards_mac,
		std::shared_ptr<BlockingQueue_Aggregator> queue_input_ptr,
		std::shared_ptr<BlockingQueue_Interpolator> queue_output_ptr);



	// ~-----methods----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

	/* Nicolò:
	 * this operator overloading make this class a functional object
	 * allowing it to perform some active operations:
	 *   - retrieve data from BlockingQueue_Aggregator
	 *   - aggregate data in a Packet object
	 *   - fill the BlockingQueue_Interpolator
	 */
	void operator() ();
};

