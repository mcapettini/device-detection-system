#pragma once

// ~-----libraries------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
#include "stdafx.h"
#include <cmath>
#include <string>
#include <iostream>
#include <vector>



// ~-----class----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

/* Nicol�:
* represent a point in a bidimensional space
* used to denote the position of a device
*/
class Coordinates
{
protected:
	// ~-----attributes-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
	double _x;
	double _y;


public:
	// ~-----constructors and destructors-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
	Coordinates();
	Coordinates(double x, double y);
	Coordinates(std::string semicolon_separated_values);

	/* Nicol�:
	 * move constructur
	 */
	Coordinates(Coordinates&& that);

	/* Nicol�:
	 * copy constructor
	 */
	Coordinates(const Coordinates& source);


	// ~-----methods----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

	/* Nicol�:
	* computes the Euclidean distance between the current point and the one provided as parameter
	*/
	double distance(Coordinates c);

	/* Nicol�:
	* computes the legth \rho of the vector identified by the current point (distance with respect to the origin of the axis)
	*/
	double radius();

	/* Nicol�:
	* computes the angular coordinate \theta of the vector identified by the current point, with respect to the origin (arctan of the ration of the two cartesian coordinates)
	*/
	double angle();

	/* Nicol�:
	* computes the angular coordinate \theta of the vector identified by the current point, with respect to the given point (arctan of the ration of the two cartesian coordinates)
	*/
	double angle(Coordinates& origin);

	/* Nicol�:
	 * sort the list of points (passed as parameter) according to the ascending \theta polar coordinate
	 * (with respect to the positive X axis)
	 */
	static std::vector<Coordinates> sort(const std::vector<Coordinates>& unordered_points);

	/* Nicol�:
	 * sort the list of points (passed as parameter) according to the ascending \theta polar coordinate
	 * (with respect to the positive X axis)
	 */
	static std::vector<Coordinates> EquispacedPoints(int numberBoards);

	/* Nicol�:
	 * external (friend) function that swap the content of
	 * two Coordinates objects
	 */
	friend void swap(Coordinates& first, Coordinates& second);



	// ~-----operators overloading--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

	/* Nicol�:
	 * move assignment operator
	 */
	Coordinates& operator= (Coordinates that);


	/* Nicol�:
	 * copy assignment operator
	 */
	//Coordinates& operator= (const Coordinates& source);

	/* Nicol�:
	 * ordering operator
	 */
	bool operator< (const Coordinates& c) const;

	/* Nicol�:
	 * comparator operator
	 */
	bool operator== (const Coordinates& c) const;


	// ~-----getters and setters----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

	/* Nicol�:
	 * setter for both _x and _y
	 */
	void update(double x, double y);

	/* Nicol�:
	 * getter for _x
	 */
	double x();

	/* Nicol�:
	 * getter for _y
	 */
	double y();
};


// ~-----costants-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
const Coordinates ORIGIN(0, 0);
const double PI = acos(-1);