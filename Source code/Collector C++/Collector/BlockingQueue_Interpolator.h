#pragma once

// ~-----libraries-----------------------------------------------------------------------------------------------------------------------------------------------------
#include "stdafx.h"
#include "Detection.h"
#include "Packet.h"



// ~-----class---------------------------------------------------------------------------------------------------------------------------------------------------------

/* Nicol�:
 * collection of data, in Packet object format, handled in thread-safe mode
 * used by Aggregator to provide data to the Interpolator
 */
class BlockingQueue_Interpolator
{
protected:
	// ~-----attributes------------------------------------------------------------------------------------------------------------------------------------------------
	int						_number_boards;
	std::deque<Packet>		_queue;
	std::mutex				_m;
	std::condition_variable _cv;



public:
	// ~-----constructors and destructors------------------------------------------------------------------------------------------------------------------------------
	BlockingQueue_Interpolator(int number_boards);



	// ~-----methods-------------------------------------------------------------------------------------------------------------------------------------------------------

	/* Nicol�:
	 * this method adds a Packet object to the BlockingQueue
	 * used by Aggregator to submit data
	 */
	void insert(Packet packet);

	/* Nicol�:
	 * this method gets a Packet objects from the BlockingQueue
	 * used by Interpolator to retrieve data
	 */
	Packet retrieve();

	/* Nicol�:
	 * this method delete all the data currently present in the queue
	 * used by Synchronizer to delete this data structure
	 */
	void clear();

	/* Nicol�:
	 * this method notifies the condition variable inside the synchronizationa data structure
	 * used by the Synchronizer to terminate all the threads waiting on this blocking queue
	 */
	void notify_threads();
};

