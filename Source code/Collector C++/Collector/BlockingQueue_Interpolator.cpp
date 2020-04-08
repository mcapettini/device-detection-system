// ~-----libraries-----------------------------------------------------------------------------------------------------------------------------------------------------
#include "stdafx.h"
#include "BlockingQueue_Interpolator.h"
#include "Synchronizer.h"


// ~-----namespaces----------------------------------------------------------------------------------------------------------------------------------------------------
using namespace std;



// ~-----constructors and destructors----------------------------------------------------------------------------------------------------------------------------------
BlockingQueue_Interpolator::BlockingQueue_Interpolator(int number_boards) : _number_boards(number_boards)
{
}



// ~-----methods-------------------------------------------------------------------------------------------------------------------------------------------------------

/* Nicolò:
 * this method adds a Packet object to the BlockingQueue
 * used by Aggregator to submit data
 */
void BlockingQueue_Interpolator::insert(Packet packet) {
	// ~-----local variables------------------------------------------------
	unique_lock<mutex> ul(_m);

	// ~-----insert in the blocking queue-----------------------------------
	_queue.push_back(packet);
#if DEBUG
	Statistics::write_interpolator(_queue.size(), "<");
#endif

	// ~-----wake up all the waiting Aggregators----------------------------
	_cv.notify_all();
}


/* Nicolò:
 * this method gets a Packet objects from the BlockingQueue
 * used by Interpolator to retrieve data
 */
Packet BlockingQueue_Interpolator::retrieve() {
	// ~-----local variables------------------------------------------------
	unique_lock<mutex> ul(_m);
	Packet output;
	int seconds = 31;

	// ~-----loop until receives stop signal from external environment------
	while (Synchronizer::_status != Synchronizer::alt) {

		// ~-----return the element in the head of the BlockingQueue--------
		if (!_queue.empty()) {
			output = _queue.front();
			_queue.pop_front();
#if DEBUG
			Statistics::write_interpolator(_queue.size(), ">");
#endif
			return output;
		}

		// ~-----no matches found, so sleep---------------------------------
		chrono::seconds sec(seconds);	//TODO: discuss the better value for waiting time
		_cv.wait_for(ul, sec);
	}
#if DEBUG
	Statistics::write_interpolator(_queue.size(), "CLOSED");
#endif

	// ~-----return code for receiving stop signal from external environment--
	return {};
}


/* Nicolò:
 * this method delete all the data currently present in the queue
 * used by Synchronizer to delete this data structure
 */
void BlockingQueue_Interpolator::clear() {
	_queue.clear();
#if DEBUG
	Statistics::write_interpolator(_queue.size(), "RESET");
#endif
}


/* Nicolò:
 * this method notifies the condition variable inside the synchronizationa data structure
 * used by the Synchronizer to terminate all the threads waiting on this blocking queue
 */
void BlockingQueue_Interpolator::notify_threads() {
	_cv.notify_all();
}