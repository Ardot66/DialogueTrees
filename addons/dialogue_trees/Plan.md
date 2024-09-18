# Plan

### Overall

1.  Make it so that nodes transfer values through connections.
2.  Add a scope system that handles defining different scopes.
3.  Ditch DialogueNodeInstances in favour of constant functions, and only saving data via variables in the heap or stack.
4.  Add global variables that are defined in somewhere like settings that can be accessed from any tree.
5.  There should be an error console to warn about things like out-of-scope variables.
6.  Figure out some kind of typing system.
7.  Add an 'await' system that allows pausing tree execution until certain conditions are met.
8.  Make more generic nodes that handle values, rather han having super specific use cases.

### Nodes

1.  Typedef: Defines a "type", which can be instanced via variable nodes. Therefore, there must be a list of type types? Like struct, class, or enum? Maybe this isn't necessary.
2.  Variable: Defines a variable based on a predefined type. Variables should either be local or global to provide more flexibility.
3.  Global Variable: Defines a variable that can be accessed by any DialogueTree, and is stored globally. Inherits Variable.
4.  Variable Getter: Gets the value of a variable and passes it through its connection. Should be able to access sub variables from things like DialogueTrees or nodes.
5.  Variable Setter: Sets the value of a variable to whatever the previous node sent. Should be able to access sub variables from things like DialogueTrees or nodes.
6.  If: Checks if the recieved value is true or false. Depending on this, continues down a specific path. Effectively the same as a switch with "true" and "false", but is a really common use case, so it should be seperate.
7.  Switch: Checks if the recieved value is equal to any constant value in a set, and continues down a specific path depending on that.
8.  Function : Defines a location that can be called and returned from, and takes parameters that are defined as local variables or constants by the user.
9.  Call Begin: Marks the beginning of a function call and splits the graph into as many branches as there are function parameters. Each branch is expected to pass a value to a function parameter.
10.  Call End: Actually calls a function. Has as many inputs as there are function parameters. The function is only called once all parameter inputs have recieved a value. Outputs the return value of the function. Programmers should be able to define functions in c# that can be accessed via call nodes, such as an output function. These external functions should either be defined in settings or directly in the DialogueTree. Maybe there could be some kind of node placed as a child of a DialogueTree that provides access to a set of functions. It should also be possible to call functions from certain types of variables (such as nodes or dialogue trees). 
11.  Return: Returns a function to the call node that called it.
12.  Label: Defines a location that can be jumped to, and that does not return. Jumps and labels should be treated exactly the same as connections from code to avoid edge cases.
13.  Jump: Jumps the program to a given label. Should only allow jumping to labels within the current scope.

### Scope Handling

Local variables need to be scoped so that they are not accessible or taking up stack space past the point where it is not guaranteed that they exist.

Simple Example:

```
                                 [Define B] - [Do Something...]
[Define A] - [Do Something...] <                                > *B and C go out of scope here and are freed from the stack* [Do Something Else...]
                                 [Define C] - [Do Something...]
```

How This Should Be Stored:

In the DialogueTreeData, each node should have a new value associated with it 'scope ends'. Scope ends is a list of integers or null that point to nodes that should go out of scope when that node recieves ANY input. During the editor, scope ends will not be needed, and instead access to local variables should be dynamically calculated.

### Variable Handling

Since variables can have different types, they'll need to have 'subnodes' that handle getting, setting, and comparing values of a certain type. Variable types should probably be defined wherever plugin settings are stored. For any given type, the programmer will need to create several scenes: 

1.   Variable setter: This node will handle UI for having the user manually set a constant value for a variable.

For comparisons, functions can be defined that are called from a function node that compare and operate on variables.

### Type Definition

While most types will be predefined by the programmer, some types would be useful to be defineable by the user. This could include things like enums or structs, which handle storing data in a particular way. All types used in a DialogueTree will be stored in a list in the DialogueTreeData.

Any type defined like this should actually be stored in settings, because they should be accessible from anywhere to allow type compatibility.

### Global Variables

Global variables would work much like defined types. They would be locally defined in a dialogue tree, but should be accessible from any DialogueTree, and stored globally.

### Plan of Action

1.   Implement all systems that need to be integrated into the core of DialogueTrees. This includes types, scope, global variables, error detection, and ditching the instance system.
2.   Start implementing nodes, starting with Typedef, then Variable, Global Variable, and Variable adjacent nodes. After that, implement if and switch, then functions, and everything else.