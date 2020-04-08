// ~-----libraries------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
#include "stdafx.h"
#include "BlockingQueue_Aggregator.h"
#include "Synchronizer.h"


// ~-----namespaces-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
using namespace std;



// ~-----constructors and destructors-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
BlockingQueue_Aggregator::BlockingQueue_Aggregator(int number_boards) :
	_number_boards(number_boards)
{
}



// ~-----methods--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

/* Nicolò:
 * this method adds a Detection object to the BlockingQueue
 * used by Socket to submit data
 */
void BlockingQueue_Aggregator::insert(Detection detection) {
	// ~-----local variables------------------------------------------------
	unique_lock<mutex> ul(_m);

	// ~-----insert in the blocking queue-----------------------------------
	_queue.push_back(detection);
#if DEBUG
	Statistics::write_aggregator(_queue.size(), "<");
#endif
	
	// ~-----wake up all the waiting Aggregators----------------------------
	_cv.notify_all();
}


/* Nicolò:
 * this method gets N Detection objects from the BlockingQueue, one for each board
 * used by Aggregator to retrieve data
 */
std::vector<Detection> BlockingQueue_Aggregator::retrieve() {
	// ~-----local variables------------------------------------------------
	unique_lock<mutex> ul(_m);
	vector<Detection> output_list;
	int seconds = 31;

	// ~-----loop until receives stop signal from external environment------
	while (Synchronizer::_status != Synchronizer::alt) {

		// ~-----scan all the BlockingQueue looking for matches-------------
		output_list.clear();
		if (count_occurences(output_list)) {
#if DEBUG
			Statistics::write_aggregator(_queue.size(), ">");
#endif
			return output_list;
		}

		// ~-----no matches found, so sleep---------------------------------
		chrono::seconds sec(seconds);	//TODO: discuss the better value for waiting time
		cv_status wakeup_reason;
		wakeup_reason = _cv.wait_for(ul, sec);

		// ~-----woke up by an insertion------------------------------------
		if (wakeup_reason == cv_status::no_timeout) {
			continue;
		}

		// ~-----woke up by timeout expiring--------------------------------
		else if (wakeup_reason == cv_status::timeout) {
			clear();
		}
	}
#if DEBUG
	Statistics::write_aggregator(_queue.size(), "CLOSED");
#endif

	// ~-----return code for receiving stop signal from external environment--
	return {};
}


/* Nicolò:
 * this method count the occurrences of the Detection objects and eventually return (as parameter)
 * the list of first N occurences found
 */
bool BlockingQueue_Aggregator::count_occurences(std::vector<Detection> &element) {
	// ~-----local variables------------------------------------------------
	map<int, set<Detection>> map_occurences;

	// ~-----check empty queue----------------------------------------------
	if (_queue.empty())
		return false;

	// ~-----count the occurences of each hash------------------------------
	// special case
	if (_number_boards == 1) {
		auto d = _queue.begin();
		element.push_back(*d);
		_queue.erase(d);
		return true;
	}

	// generic case
	for (auto iterator = _queue.begin(); iterator != _queue.end(); iterator++) {

		// ~-----new hash---------------------------------------------------
		if (map_occurences.find(iterator->hash()) == map_occurences.end()) {
			set<Detection> internal_set;
			internal_set.insert(*iterator);
			map_occurences.insert(make_pair(iterator->hash(), internal_set));
		}
		
		// ~-----hash already present---------------------------------------
		else {
			// retrieve the map element
			map<int, set<Detection>>::iterator map_iterator = map_occurences.find(iterator->hash());

			// add the new Detection to the set (can cause exception)
			map_iterator->second.insert(*iterator);

			// check if is reached the number of boards
			if (map_iterator->second.size() == _number_boards) {
				copy(map_iterator->second.begin(), map_iterator->second.end(), back_inserter(element));
				
				for (auto set_iterator = map_iterator->second.begin(); set_iterator != map_iterator->second.end(); set_iterator++) {
					auto target = find(_queue.begin(), _queue.end(), *set_iterator);
					_queue.erase(target);
				}

				return true;
			}

		}
	}

	// ~-----return code for failure-----------------------------------------
	return false;
}


/* Nicolò:
 * this method delete all the data currently present in the queue
 * used by Synchronizer to delete this data structure
 */
void BlockingQueue_Aggregator::clear() {
	_queue.clear();
#if DEBUG
	Statistics::write_aggregator(_queue.size(), "RESET");
#endif
}


/* Nicolò:
 * this method notifies the condition variable inside the synchronizationa data structure
 * used by the Synchronizer to terminate all the threads waiting on this blocking queue
 */
void BlockingQueue_Aggregator::notify_threads() {
	_cv.notify_all();
}


// ~-----getters and setters--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------