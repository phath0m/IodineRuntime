## ```inspect``` module

Provides functions for inspecting and manipulating live objects.
### Functions

#### func ```getargspec``` ()
___
Returns a tuple containing the names of all function parameters
#### func ```getattribute``` (obj, attr)
___
Gets a specific attribute of an object@returns Object
#### func ```getattributes``` (obj)
___
Gets all attributes of an object@returns Dict
#### func ```getbytecode``` (callable)
___
Decompiles a function to get its bytecode@returns List
#### func ```getcontracts``` (obj)
___
Gets all contracts of an object@returns List
#### func ```getinterfaces``` (obj)
___
Gets all contracts of an object@returns List
#### func ```getmembers``` (obj)
___
Gets all attributes of an object@returns Dict
#### func ```hasattribute``` (obj, attr)
___
Checks whether or not an object has a specific attribute@returns Bool
#### func ```isbuiltin``` (obj)
___
Checks if an object is a builtin method@returns Bool
#### func ```isclass``` (obj)
___
Checks if an object is a class@returns Bool
#### func ```isfunction``` (obj)
___
Checks if an object is a method, function or closure@returns Bool
#### func ```isgeneratormethod``` (obj)
___
Checks if an object is a generator@returns Bool
#### func ```ismethod``` (obj)
___
Checks if an object is a method@returns Bool
#### func ```ismodule``` (obj)
___
Checks if an object is a module@returns Bool
#### func ```isproperty``` (obj)
___
Checks if an object is a method property@returns Bool
#### func ```istype``` (obj)
___
Checks if an object is a type@returns Bool
#### func ```loadmodule``` (path)
___
Loads a module@returns Module
#### func ```setattribute``` (obj, attrthe, value)
___
Sets a specific attribute of an object


