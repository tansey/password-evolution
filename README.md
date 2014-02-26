# Markov Chain Password Evolution via NEAT
This is a version of the SharpNEAT (v2.1) framework that has been adapted to evolve Markov chains instead of neural networks. The goal of this project is to evolve Markov chains that can better model the distributions of user-created passwords in online databases.

## Acessing password data
The password data was too large to store on Github, since some of the lists are hundreds of MB in size. You can find all the data for the passwords at (this dropbox folder)[https://www.dropbox.com/sh/pxr2yl4zzeldc0h/dE3AwqXPEi].

## Changes to SharpNEAT
To accomodate Markov chains, I have made some changes to the underlying framework. It is my hope that keeping track of the core changes will help guide the next iteration of SharpNEAT to make it more modular and flexible regarding less traditional structures.

### NetworkXmlIO
Changed the call to GetActivationFunction to use a dictionary instead of a hardcoded switch statement. Added an AddActivationFunction method that enables the user to add a new activation function that can leverage the existing IO plumbing without having to modify the core framework.

#### Other recommended changes in the next version of SharpNEAT
- Utilize generics more, especially regarding nodes and activation functions. Not every kind of node contains an array of doubles for its auxilary state, takes a double as input, and outputs a double.

- Remove the IBlackBox dependencies in INeatExperiment

