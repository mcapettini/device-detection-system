// ~-----libraries------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
#include "stdafx.h"
#include "SocketStub.h"
#include "Synchronizer.h"



// ~-----namespaces-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
using namespace std;



// ~-----constructors and destructors-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
SocketStub::SocketStub(int number_boards,
	shared_ptr<BlockingQueue_Aggregator> bq_aggregator_ptr,
	map<string, Coordinates> boards_position) :
		_number_boards(number_boards), _bq_aggregator_ptr(bq_aggregator_ptr), _boards_position(boards_position)
{

}



// ~-----methods--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

string SocketStub::randMAC() {
	string mac;
	random_device rd;

	for (int i = 0; i < 12; i++) {
		int x = rd() % 16;
		if (x < 10) {
			mac += to_string(x);
		}
		else {
			char letter = 'a' + x - 10;
			mac += toupper(letter);
		}
		if (mac.length() < 17 && mac.length() > 2 && (i + 1) % 2 == 0)
			mac += ".";
		if (mac.length() == 2)
			mac += ".";
	}
	return mac;
}


map<string, Coordinates> SocketStub::randBoards(int number_boards) {
	map<string, Coordinates> boards_position;
	random_device rd;

	// ~-----creates boards-------------------------------------------------
	for (int i = 0; i < number_boards; i++) {
		int x = rd() % 20;
		x -= 10;
		int y = rd() % 20;
		y -= 10;
		Coordinates c(x, y);
		string mac = randMAC();

		boards_position.insert(make_pair(mac, c));
	}

	// ~-----order vertices-------------------------------------------------
	vector<Coordinates> unordered_points, vertices_ordered;
	transform(boards_position.begin(), boards_position.end(), back_inserter(unordered_points), [](const auto& val) {return val.second; });

	vertices_ordered = Coordinates::sort(unordered_points);

	// ~-----console output-------------------------------------------------
	for (auto v : vertices_ordered) {
		string  mac = "";

		for (auto row : boards_position) {
			if (row.second == v) {
				mac = row.first;
				break;
			}
		}

#if DEBUG
		cout << "MAC= " << mac << " coordinates=(" << v.x() << ":" << v.y() << ")" << flush << endl;
#endif
	}
#if DEBUG
	cout << "======================================================================" << flush << endl;
#endif

	return boards_position;
}


string SocketStub::randBoards_toConfString(int number_boards) {
	map<string, Coordinates> boards_position;
	random_device rd;
	string out = "";
#if DEBUG
	string polygon = "q1=Poligono(";
	int index = 1;
#endif

	// ~-----creates boards-------------------------------------------------
	for (int i = 0; i < number_boards; i++) {
		int x = rd() % 20;
		x -= 10;
		int y = rd() % 20;
		y -= 10;
		Coordinates c(x, y);
		string mac = randMAC();

		boards_position.insert(make_pair(mac, c));
	}

	// ~-----order vertices-------------------------------------------------
	vector<Coordinates> unordered_points, vertices_ordered;
	transform(boards_position.begin(), boards_position.end(), back_inserter(unordered_points), [](const auto& val) {return val.second; });
	
	vertices_ordered = Coordinates::sort(unordered_points);

	// ~-----configuration string output------------------------------------
	for (auto v : vertices_ordered) {
		string  mac = "";


		for (auto row : boards_position) {
			if (row.second == v) {
				mac = row.first;
				break;
			}
		}

		out += mac + "|" + to_string(v.x()) + "|" + to_string(v.y()) + "_";
#if DEBUG
		/*setup += "V" + to_string(index) + "=(" + to_string(v.x()) + "," + to_string(v.y()) + ")\nImpColore(" + "V" + to_string(index) + ",\"Black\")\n"; //V4=(-5,5)\nImpColore(V4,\"Black\")\nq1=Poligono(V1,V2,V3,V4)\nImpColore(q1,\"Black\")\n
		polygon += "V" + to_string(index) + ",";
		index++;*/
#endif
	}
	out = out.substr(0, out.length() - 1);
#if DEBUG
	/*polygon = polygon.substr(0, polygon.length() - 1);
	polygon += ")\nImpColore(q1,\"Black\")\n";
	setup += polygon;*/
#endif

	return out;
}




// ~-----operators overloading------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

/* Nicolò:
 * this operator overloading make this class a functional object
 * allowing it to perform some active operations:
 *   - generate several threads
 *   - generate a probable position fo a detected device (according to the boards position)
 *   - introducing an error in the measurements
 *   - inserting data into BlockingQueue_Aggregator
 */
void SocketStub::operator() () {
	// ~-----local variables-----------------------------------------------------
	// common attributes
	int hash;
	string MACsmartphone;
	int sequence_number;
	string SSID;
	// unique attributes
	string MACboard;
	double RSSI;
	chrono::milliseconds timestamp;
	// utilities
	random_device rd;
	string console_output = "";

	// ~-----generate random values----------------------------------------------
	for (int i = 0; i < 200; i++) {
		hash = rd() % 100000 + 100000;
		console_output = "hash= " + to_string(hash);
		MACsmartphone = randMAC();
		console_output += " MACsmartphone= " + MACsmartphone;
		if ((int)(rd() % 100) > 50) { // 50% probability
			sequence_number = rd() % 1000;
			SSID = "";
			for (int i = 0; i < rd() % 6 + 10; i++)
				SSID += (char)rd() % ('Z' - 'a') + 'a';
			console_output += " address= PRIVATE";
		}
		else {
			sequence_number = -1;
			SSID = "";
			console_output += " address= PUBLIC";
		}

		double x = (rd() % 20); x -= 10;
		double y = (rd() % 20); y -= 10;
		Coordinates smartphone(x, y);
		console_output += " real_position= (" + to_string(x) + ":" + to_string(y) + ")";
#if DEBUG
		unique_lock<mutex> ul(_m_console);
		cout << console_output << flush << endl;
		/*geogebra_ofstream.open(geogebra_filename + "_" + MACsmartphone + ".txt");
		geogebra_ofstream << setup;
		geogebra_ofstream << "REAL" << "=(" << to_string(x) << "," << to_string(y) << ")\nImpColore(REAL,\"Red\")\n";
		geogebra_ofstream.close();*/
		ul.unlock();
#endif

		for (auto board : _boards_position) {
			console_output = "";
			console_output += "   MACboard= " + board.first;

			RSSI = (double)-20 * log10(smartphone.distance(board.second)) + (double)(-47);	// correct computation
			RSSI += (rd() % 5); RSSI -= 2.5;	// introduce an error
			console_output += " RSSI= " + to_string(RSSI);

			chrono::milliseconds temp(rd() % 10000);
			timestamp = temp + chrono::duration_cast<chrono::milliseconds>(chrono::system_clock::now().time_since_epoch());
			//console_output += "Timestamp: " + to_string(timestamp) + endl;

			Detection d(hash, board.first, MACsmartphone, timestamp, RSSI);
			if (sequence_number != -1)
				d.set_private_MACaddress(sequence_number, SSID, 0);
			_bq_aggregator_ptr->insert(d);

#if DEBUG
			ul.lock();
			cout << console_output << flush << endl;
			ul.unlock();
#endif
		}
#if DEBUG
		_m_console.lock();
		cout << "----------------------------------------------------------------------" << flush << endl;
		_m_console.unlock();
#endif
	}
}