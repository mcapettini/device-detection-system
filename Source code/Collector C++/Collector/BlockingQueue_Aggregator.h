#pragma once

// ~-----libraries-----------------------------------------------------------------------------------------------------------------------------------------------------
#include "stdafx.h"
#include "Detection.h"



// ~-----class---------------------------------------------------------------------------------------------------------------------------------------------------------

/* Nicolò:
 * collection of data, in Detection object format, handled in thread-safe mode
 * used by Socket to provide data to the Aggregator
 */
class BlockingQueue_Aggregator
{
protected:
	// ~-----attributes------------------------------------------------------------------------------------------------------------------------------------------------
	int						_number_boards;
	std::deque<Detection>	_queue;
	std::mutex				_m;
	std::condition_variable _cv;



public:
	// ~-----constructors and destructors------------------------------------------------------------------------------------------------------------------------------
	BlockingQueue_Aggregator(int number_boards);



	// ~-----methods-------------------------------------------------------------------------------------------------------------------------------------------------------
	
	/* Nicolò:
	 * this method adds a Detection object to the BlockingQueue
	 * used by Socket to submit data
	 */
	void insert(Detection detection);

	/* Nicolò:
	 * this method gets N Detection objects from the BlockingQueue, one for each board
	 * used by Aggregator to retrieve data
	 */
	std::vector<Detection> retrieve();

	/* Nicolò:
	 * this method delete all the data currently present in the queue
	 * used by Synchronizer to delete this data structure
	 */
	void clear();

	/* Nicolò:
	 * this method notifies the condition variable inside the synchronizationa data structure
	 * used by the Synchronizer to terminate all the threads waiting on this blocking queue
	 */
	void notify_threads();


private:
	/* Nicolò:
	 * this method count the occurrences of the Detection objects and return (as parameter)
	 * the list of first N occurences found
	 */
	bool count_occurences(std::vector<Detection> &element);



	// ~-----getters and setters---------------------------------------------------------------------------------------------------------------------------------------

};

