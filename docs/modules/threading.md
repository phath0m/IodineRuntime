## ```threading``` module

### Functions

#### func ```sleep``` (t)
___
Suspends the current thread for t milliseconds.

#### class ```Mutex``` ()
___
A simple mutual exclusion lock.
##### Methods

| Name                    | Arguments         | Description |
| ----------------------- | ----------------- | ----------- |
|release| **None** |Releases the mutex, allowing any threads blocked by this lock to continue.|
|acquire| **None** |Enters the critical section, blocking all threads until release the lock is released.|
|synchronize| callable |Acquires a lock, then executes the supplied argument before releasing the lock.|


#### class ```Thread``` (func)
___
Creates and controls a thread.
##### Methods

| Name                    | Arguments         | Description |
| ----------------------- | ----------------- | ----------- |
|alive| **None** |Returns true if this thread is alive, false if it is not.|
|join| **None** |Joins this thread with the calling thread|
|abort| **None** |Terminates the thread.|
|start| **None** |Starts the thread.|



