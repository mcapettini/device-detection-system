// ~-----libraries------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
#include "stdafx.h"
#include "Aggregator.h"
#include "Synchronizer.h"


// ~-----namespaces-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
using namespace std;



// ~-----constructors and destructors-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
Aggregator::Aggregator(int number_boards,
	std::vector<string> boards_mac,
	std::shared_ptr<BlockingQueue_Aggregator> queue_input_ptr,
	std::shared_ptr<BlockingQueue_Interpolator> queue_output_ptr) :
		_number_boards(number_boards),
		_boards_mac(boards_mac),
		_queue_input(queue_input_ptr),
		_queue_output(queue_output_ptr)
{
}



// ~-----methods--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

/* Nicolò:
 * this operator overloading make this class a functional object
 * allowing it to perform some active operations:
 *   - retrieve data from BlockingQueue_Aggregator
 *   - aggregate data in a Packet object
 *   - fill the BlockingQueue_Interpolator
 */
void Aggregator::operator() () {
	// ~-----local variables------------------------------------------------
	vector<Detection> detections;


	// ~-----loop until receives stop signal from external environment------
	while (Synchronizer::_status != Synchronizer::alt) {

		try
		{
			// ~-----retrieve data from BlockingQueue_Aggregator--------------------
			detections = _queue_input->retrieve();
			if (detections.empty())	// thread termination
				continue;


			// ~-----check coherence with the provided configuration----------------
			if (detections.size() != _number_boards)
				Synchronizer::report_error(Synchronizer::AggregatorError, "The number of Detections differs from the number of boards");

			for (auto iter = detections.begin(); iter != detections.end(); iter++)
				if (find(_boards_mac.begin(), _boards_mac.end(), iter->MACaddress_board()) == _boards_mac.end())
					Synchronizer::report_error(Synchronizer::BadConfiguration, "Received data from unexpected board");


			// ~-----create Packet object-------------------------------------------
			auto iterator = detections.begin();
			Packet packet(iterator->hash(), iterator->MACaddress_device());
			if (iterator->is_private()) {
				packet.set_private_MACaddress(iterator->sequence_number(), iterator->SSID(), iterator->fingerprint());
			}

			for (iterator = detections.begin(); iterator != detections.end(); iterator++) {
				packet.add(iterator->MACaddress_board(), iterator->timestamp(), iterator->RSSI());
			}


			// ~-----insert data in the BlockingQueue_Interpolator------------------
			_queue_output->insert(packet);

		}
		catch (const std::exception& ex)
		{
			stringstream ss;
			ss << "General non-catched exception - " << ex.what();
			Synchronizer::report_error(Synchronizer::AggregatorError, ss.str());
		}
	}
}