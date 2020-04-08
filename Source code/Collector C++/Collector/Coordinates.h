#pragma once

// ~-----libraries------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
#include "stdafx.h"
#include <cmath>
#include <string>
#include <iostream>
#include <vector>



// ~-----class----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

/* Nicolò:
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

	/* Nicolò:
	 * move constructur
	 */
	Coordinates(Coordinates&& that);

	/* Nicolò:
	 * copy constructor
	 */
	Coordinates(const Coordinates& source);


	// ~-----methods----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

	/* Nicolò:
	* computes the Euclidean distance between the current point and the one provided as parameter
	*/
	double distance(Coordinates c);

	/* Nicolò:
	* computes the legth \rho of the vector identified by the current point (distance with respect to the origin of the axis)
	*/
	double radius();

	/* Nicolò:
	* computes the angular coordinate \theta of the vector identified by the current point, with respect to the origin (arctan of the ration of the two cartesian coordinates)
	*/
	double angle();

	/* Nicolò:
	* computes the angular coordinate \theta of the vector identified by the current point, with respect to the given point (arctan of the ration of the two cartesian coordinates)
	*/
	double angle(Coordinates& origin);

	/* Nicolò:
	 * sort the list of points (passed as parameter) according to the ascending \theta polar coordinate
	 * (with respect to the positive X axis)
	 */
	static std::vector<Coordinates> sort(const std::vector<Coordinates>& unordered_points);

	/* Nicolò:
	 * sort the list of points (passed as parameter) according to the ascending \theta polar coordinate
	 * (with respect to the positive X axis)
	 */
	static std::vector<Coordinates> EquispacedPoints(int numberBoards);

	/* Nicolò:
	 * external (friend) function that swap the content of
	 * two Coordinates objects
	 */
	friend void swap(Coordinates& first, Coordinates& second);



	// ~-----operators overloading--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

	/* Nicolò:
	 * move assignment operator
	 */
	Coordinates& operator= (Coordinates that);


	/* Nicolò:
	 * copy assignment operator
	 */
	//Coordinates& operator= (const Coordinates& source);

	/* Nicolò:
	 * ordering operator
	 */
	bool operator< (const Coordinates& c) const;

	/* Nicolò:
	 * comparator operator
	 */
	bool operator== (const Coordinates& c) const;


	// ~-----getters and setters----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

	/* Nicolò:
	 * setter for both _x and _y
	 */
	void update(double x, double y);

	/* Nicolò:
	 * getter for _x
	 */
	double x();

	/* Nicolò:
	 * getter for _y
	 */
	double y();
};


// ~-----costants-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
const Coordinates ORIGIN(0, 0);
const double PI = acos(-1);