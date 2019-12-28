## ```__builtins__``` module

Provides access to iodine builtin functions and classes.
### Functions

#### func ```chr``` (num)
___
Returns the character representation of a specified integer.
#### func ```compile``` (source)
___
Compiles a string of iodine code, returning a callable object.
#### func ```enumerate``` (iterable)
___
Maps an iterable object to a list, with each element in the list being a tuple containing an index and the object associated with that index in the supplied iterable object.
#### func ```eval``` (source)
___
Evaluates a string of Iodine source code.
#### func ```filter``` (iterable, callable)
___
Iterates over the specified iterable, passing the result of each iteration to the specified callable. If the callable returns true, the result is appended to a list that is returned to the caller.
#### func ```globals``` ()
___
Returns a dictionary of all global variables.
#### func ```hex``` (obj)
___
Returns hexadecimal representation of a specified object,supports both Bytes and Str objects.
#### func ```id``` (obj)
___
Returns a unique identifier for the supplied argument. 
#### func ```input``` (prompt)
___
Reads from the standard input stream. Optionally displays the specified prompt.
#### func ```invoke``` (callable, dict)
___
Invokes the specified callable under a new Iodine context.Optionally uses the specified dict as the instance's global symbol table.
#### func ```len``` (countable)
___
Returns the length of the specified object. If the object does not implement __len__, an AttributeNotFoundException is raised.
#### func ```loadmodule``` (name)
___
Loads an iodine module.
#### func ```locals``` ()
___
Returns a dictionary of all local variables.
#### func ```map``` (iterable, callable)
___
Iterates over the specified iterable, passing the result of each iteration to the specified callable. The result of the specified callable is added to a list that is returned to the caller.
#### func ```open``` (file, mode)
___
Opens up a file using the specified mode, returning a new stream object.<br><strong>Supported modes</strong><br><li> r - Read<li> w - Write<li> a - Append<li> b - Binary 
#### func ```ord``` (char)
___
Returns the numeric representation of a character.
#### func ```print``` (*object)
___
Prints a string to the standard output streamand appends a newline character.
#### func ```property``` (getter, setter)
___
Returns a new Property object.
#### func ```range``` (start, end, step)
___
Returns an iterable sequence containing [n] items, starting with 0 and incrementing by 1, until [n] is reached.
#### func ```reduce``` (iterable, callable, default)
___
Reduces all members of the specified iterable by applying the specified callable to each item left to right. The callable passed to reduce receives two arguments, the first one being the result of the last call to it and the second one being the current item from the iterable.
#### func ```reload``` (module)
___
Reloads an iodine module.
#### func ```repr``` (object)
___
Returns a string representation of the specified object, which is obtained by calling its __repr__ function. If the object does not implement the __repr__ function, its default string representation is returned.
#### func ```require``` ()
___
Internal function used by the 'use' statement, do not call this directly.
#### func ```sort``` (iterable, [key])
___
Returns an sorted tuple created from an iterable sequence. An optional function can be provided that can be used to sort the iterable sequence.
#### func ```sum``` (iterable, default)
___
Reduces the iterable by adding each item together, starting with [default].
#### func ```type``` (object)
___
Returns the type definition of the specified object.
#### func ```typecast``` (type, object)
___
Performs a sanity check, verifying that the specified [object] is an instance of [type]. If the test fails, a TypeCastException is raised.
#### func ```zip``` (iterables)
___
Iterates over each iterable in [iterables], appending every item to a tuple, that is then appended to a list which is returned to the caller.

#### class ```BigInt``` ()
___
An arbitrary size integer


#### class ```Dict``` ([values])
___
A dictionary containing a list of unique keys and an associated value
##### Methods

| Name                    | Arguments         | Description |
| ----------------------- | ----------------- | ----------- |
|clear| **None** |Clears the dictionary, removing all items.|
|get| key |Returns the value specified by [key], raising a KeyNotFound exception if the given key does not exist.|
|remove| key |Removes a specified entry from the dictionary, raising a KeyNotFound exception if the given key does not exist.|
|contains| key |Tests to see if the dictionary contains a key, returning true if it does.|
|set| key, value |Sets a key to a specified value, if the key does not exist, it will be created.|


#### class ```File``` ()
___
An object supporting read or write operations (Typically a file)
##### Methods

| Name                    | Arguments         | Description |
| ----------------------- | ----------------- | ----------- |
|readln| **None** |Reads a single line from the underlying stream.|
|writeln| obj |Writes an object to the stream, appending a new line character to the end of the file.|
|write| obj |Writes an object to the underlying stream.|
|readall| **None** |Reads all text.|
|read| n |Reads [n] bytes from the underlying stream.|
|close| **None** |Closes the stream.|
|flush| **None** |Flushes the underlying stream.|


#### class ```Float``` ()
___
A double precision floating point


#### class ```Int``` ()
___
A 64 bit signed integer
##### Methods

| Name                    | Arguments         | Description |
| ----------------------- | ----------------- | ----------- |
|times| callable |Invokes the supplied callable n times, with n being the value of this integer|


#### class ```List``` ()
___
A mutable sequence of objects
##### Methods

| Name                    | Arguments         | Description |
| ----------------------- | ----------------- | ----------- |
|reduce| callable, default |Reduces all members of the specified iterable by applying the specified callable to each item left to right. The callable passed to reduce receives two arguments, the first one being the result of the last call to it and the second one being the current item from the iterable.|
|each| func |Iterates through each element in the collection.|
|last| value |Returns the last item in this collection.|
|map| callable |Iterates over the specified iterable, passing the result of each iteration to the specified callable. The result of the specified callable is added to a list that is returned to the caller.|
|prepend| item |Prepends an item to the beginning of the list.|
|find| item |Returns the index of the first occurance of the supplied argument, returning -1  if the supplied argument cannot be found.|
|filter| callable |Iterates over the specified iterable, passing the result of each iteration to the specified callable. If the callable returns true, the result is appended to a list that is returned to the caller.|
|rfind| item |Returns the index of the last occurance of the supplied argument, returning -1  if the supplied argument cannot be found.|
|contains| item |Returns true if the supplied argument can be fund within the list.|
|remove| item |Removes an item from the list, raising a KeyNotFound exception if the list does not contain [item].|
|append| *args |Appends each argument to the end of the list|
|removeat| index |Removes an item at a specified index.|
|index| item |Returns the index of the first occurance of the supplied argument, raising a KeyNotFound exception  if the supplied argument cannot be found.|
|first| value |Returns the first item in this collection.|
|clear| **None** |Clears the list, removing all items from it.|
|rindex| item |Returns the index of the last occurance of the supplied argument, raising a KeyNotFound exception  if the supplied argument cannot be found.|
|appendrange| iterable |Iterates through the supplied arguments, adding each item to the end of the list.|
|discard| item |Removes an item from the list, returning true if success, otherwise, false.|


#### class ```Str``` ()
___
An immutable string of UTF-16 characters
##### Methods

| Name                    | Arguments         | Description |
| ----------------------- | ----------------- | ----------- |
|isalpha| **None** |Returns true if all characters in this string are letters.|
|replace| str1, str2 |Returns a new string where call occurances of [str1] have been replaced with [str2].|
|index| substring |Returns the index of the first occurance of a string within this string. Raises KeyNotFound exception if the specified substring does not exist.|
|isalnum| **None** |Returns true if all characters in this string are letters or digits.|
|issymbol| **None** |Returns true if all characters in this string are symbols.|
|join| *args |Joins all arguments together, returning a string where this string has been placed between all supplied arguments|
|startswith| value |Returns true if the string starts with the specified value.|
|rindex| substring |Returns the index of the last occurance of a string within this string. Raises KeyNotFound exception if the specified substring does not exist.|
|trim| **None** |Returns a string where all leading whitespace characters have been removed.|
|map| callable |Iterates over the specified iterable, passing the result of each iteration to the specified callable. The result of the specified callable is added to a list that is returned to the caller.|
|endswith| value |Returns true if the string ends with the specified value.|
|each| func |Iterates through each element in the collection.|
|substr| start, [end] |Returns a substring contained within this string.@returns The substring between start and end|
|upper| **None** |Returns the uppercase representation of this string|
|rjust| n, [c] |Returns a string that has been justified by [n] characters to left.|
|contains| value |Returns true if the string contains the specified value. |
|lower| **None** |Returns the lowercase representation of this string|
|filter| callable |Iterates over the specified iterable, passing the result of each iteration to the specified callable. If the callable returns true, the result is appended to a list that is returned to the caller.|
|rfind| substring |Returns the index of the last occurance of a string within this string. Returns -1 if the specified substring does not exist.|
|isdigit| **None** |Returns true if all characters in this string are digits.|
|last| value |Returns the last item in this collection.|
|iswhitespace| **None** |Returns true if all characters in this string are white space characters.|
|split| seperator |Returns a list containing every substring between [seperator].|
|find| substring |Returns the index of the first occurance of a string within this string. Returns -1 if the specified substring does not exist.|
|reduce| callable, default |Reduces all members of the specified iterable by applying the specified callable to each item left to right. The callable passed to reduce receives two arguments, the first one being the result of the last call to it and the second one being the current item from the iterable.|
|ljust| n, [c] |Returns a string that has been justified by [n] characters to right.|
|first| value |Returns the first item in this collection.|


#### class ```StringBuffer``` ()
___
A mutable string of UTF-16 characters
##### Methods

| Name                    | Arguments         | Description |
| ----------------------- | ----------------- | ----------- |
|prepend| item |Prepends text to the beginning of the string buffer.|
|clear| **None** |Clears the string buffer.|
|append| *args |Appends each argument to the end of the string buffer.|


#### class ```Tuple``` ()
___
An immutable collection of objects
##### Methods

| Name                    | Arguments         | Description |
| ----------------------- | ----------------- | ----------- |
|filter| callable |Iterates over the specified iterable, passing the result of each iteration to the specified callable. If the callable returns true, the result is appended to a list that is returned to the caller.|
|first| value |Returns the first item in this collection.|
|map| callable |Iterates over the specified iterable, passing the result of each iteration to the specified callable. The result of the specified callable is added to a list that is returned to the caller.|
|last| value |Returns the last item in this collection.|
|reduce| callable, default |Reduces all members of the specified iterable by applying the specified callable to each item left to right. The callable passed to reduce receives two arguments, the first one being the result of the last call to it and the second one being the current item from the iterable.|
|each| func |Iterates through each element in the collection.|



