// ~-----libraries------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
#include "stdafx.h"
#include "Coordinates.h"


// ~-----namespaces-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
using namespace std;



// ~-----constructors and destructors-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

Coordinates::Coordinates() {
	_x = 0;
	_y = 0;
}

Coordinates::Coordinates(double x, double y) {
	this->_x = x;
	this->_y = y;
}

Coordinates::Coordinates(std::string semicolon_separated_values) {
	int semicolon_index = semicolon_separated_values.find(":");

	if (semicolon_index <= 0 || semicolon_index >= semicolon_separated_values.length() - 1)
		throw std::exception("The provided string do not contains ':' delimiter");

	std::string x = semicolon_separated_values.substr(0, semicolon_index);
	std::string y = semicolon_separated_values.substr(semicolon_index + 1, semicolon_separated_values.length() - 1);

	this->_x = atoi(x.c_str());
	this->_y = atoi(y.c_str());
}

/* Nicolò:
 * move constructur
 */
Coordinates::Coordinates(Coordinates&& that) {
	swap(*this, that);
}

/* Nicolò:
 * copy constructor
 */
Coordinates::Coordinates(const Coordinates& source) {
	this->_x = source._x;
	this->_y = source._y;
}



// ~-----methods--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

/* Nicolò:
* computes the Euclidean distance between the current point and the one provided as parameter
*/
double Coordinates::distance(const Coordinates c) {
	return sqrt(pow(this->_x - c._x, 2) + pow(this->_y - c._y, 2));
}

/* Nicolò:
* computes the legth \rho of the vector identified by the current point (distance with respect to the origin of the axis)
*/
double Coordinates::radius() {
	return distance(ORIGIN);
}

/* Nicolò:
* computes the angular coordinate \theta of the vector identified by the current point (arctan of the ration of the two cartesian coordinates)
*/
double Coordinates::angle() {
	double angle = atan2(this->_y, this->_x);
	if (angle < 0)
		angle += 2*PI;
	return angle;
}

/* Nicolò:
* computes the angular coordinate \theta of the vector identified by the current point, with respect to the given point (arctan of the ration of the two cartesian coordinates)
*/
double Coordinates::angle(Coordinates& new_origin) {
	double angle = atan2(this->_y - new_origin._y, this->_x - new_origin._x);
	if (angle < 0)
		angle += 2*PI;
	return angle;
}

/* Nicolò:
 * sort the list of points (passed as parameter) according to the ascending \theta polar coordinate
 * (with respect to the positive X axis)
 */
std::vector<Coordinates> Coordinates::sort(const std::vector<Coordinates>& unordered_points)
{
	// ~-----local variables------------------------------------------------
	double x_sum = 0, y_sum = 0;
	vector<Coordinates> ordered_points(unordered_points);


	// ~-----compute centroid-----------------------------------------------
	for (auto point : unordered_points) {
		//ordered_points.push_back(point);
		x_sum += point.x();
		y_sum += point.y();
	}

	Coordinates centroid(x_sum / unordered_points.size(), y_sum / unordered_points.size());	// TODO: this solution works only for convex polygons


	// ~-----sort in anti-clockwise order, according to ascending angle-----
	std::sort(ordered_points.begin(),
		ordered_points.end(),
		[&](Coordinates a, Coordinates b) {return a.angle(centroid) < b.angle(centroid); }
	);

	return ordered_points;
}

/* Nicolò:
 * sort the list of points (passed as parameter) according to the ascending \theta polar coordinate
 * (with respect to the positive X axis)
 */
static vector<Coordinates> EquispacedPoints(int numberBoards)
{
	// local variables
	double r = 5, angle_quantum, angle_sum = 0;
	vector<Coordinates> list;

	// split \pi into N equivalent angles
	angle_quantum = 2 * PI / numberBoards;

	// compute Re[z] and Im[z] for each point
	angle_sum = angle_quantum / 2;
	for (int i = 0; i < numberBoards; i++)
	{
		double x = r * cos(angle_sum);
		double y = r * sin(angle_sum);
		Coordinates c(x, y);

		list.push_back(c);
		angle_sum += angle_quantum;
	}

	// return computed points
	return list;
}

/* Nicolò:
 * external (friend) function that swap the content of
 * two Coordinates objects
 */
void swap(Coordinates& first, Coordinates& second) {
	std::swap(first._x, second._x);
	std::swap(first._y, second._y);
}


// ~-----operators overloading------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

/* Nicolò:
 * move assignment operator
 */
Coordinates& Coordinates::operator= (Coordinates that) {
	swap(*this, that);
	return *this;
}


/* Nicolò:
 * copy assignment operator
 */
/*Coordinates& Coordinates::operator= (const Coordinates& source) {
	if (this != &source) {
		this->_x = source._x;
		this->_y = source._y;
	}
	return *this;
}*/


/* Nicolò:
 * ordering operator
 */
bool Coordinates::operator< (const Coordinates& c) const {
	if (this->_x < c._x) {
		return true;
	}
	else if (this->_x > c._x) {
		return false;
	}
	else {
		if (this->_y < c._y)
			return true;
		else if (this->_y > c._y)
			return false;
		else
			return false;
	}
}

/* Nicolò:
 * comparator operator
 */
bool Coordinates::operator== (const Coordinates& c) const {
	return (this->_x == c._x) && (this->_y == c._y);
}


// ~-----getters and setters--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

/* Nicolò:
 * setter for both _x and _y
 */
void Coordinates::update(double x, double y) {
	_x = x;
	_y = y;
}

/* Nicolò:
 * getter for _x
 */
double Coordinates::x() {
	return _x;
}

/* Nicolò:
 * getter for _y
 */
double Coordinates::y() {
	return _y;
}