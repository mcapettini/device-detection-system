#pragma once

// ~-----libraries------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
#include <iostream>
#include "stdafx.h"



// ~-----class----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

/* Nicol�:
 * emulate the C# properties:
 * exhibit an attribute syntax, but hide a couple of methods (getter and setter)
 */
template <typename T>
class Property
{
protected:
	// ~-----attributes-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
	T value;


public:
	// ~-----constructors and destructors-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
	Property() {}
	virtual ~Property() {}


	// ~-----methods----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
	
	/* Nicol�:
	 * getter method (return the value of the attribute)
	 */
	virtual T & operator = (const T &f) {
		return value = f;
	}

	/* Nicol�:
	 * setter method (assign the given value to the attribute)
	 */
	virtual operator T const & () const {
		return value;
	}

	/* Nicol�:
	* output on 'cout' or 'cerr' (insert the value in the ostream)
	*/
	friend std::ostream& operator<< (std::ostream& stream, const Property<T>& el) {
		stream << el.value;
		return stream;
	}

	/* Nicol�:
	* input from 'cin' (insert the value in the istream)
	*/
	friend std::istream& operator>> (std::istream& stream, Property<T>& el) {
		T temp;
		stream >> temp;
		el.value = temp;
		return stream;
	}
};

