## ```os``` module

Provides a portable way for interacting with the host operating system
### Functions

#### func ```call``` (executable, [args], [useShell])
___
Executes program, waiting for it to exit and returning its exit code.
#### func ```getcwd``` ()
___
Returns the current working directory.
#### func ```getenv``` (env)
___
Returns the value of an environmental variable.
#### func ```getlogin``` ()
___
Returns the login name of the current user.
#### func ```list``` (path)
___
Returns a list of all subfiles in a directory.
#### func ```mkdir``` (path)
___
Creates a new directory.
#### func ```putenv``` (env, value)
___
Sets an environmental variable to a specified value
#### func ```rmdir``` (path)
___
Removes an empty directory.
#### func ```rmtree``` (path)
___
Removes an directory, deleting all subfiles.
#### func ```setcwd``` (cwd)
___
Sets the current working directory.
#### func ```system``` (commmand)
___
Executes a command using the default shell.
#### func ```unlink``` (path)
___
Removes a file from the filesystem.


