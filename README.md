# JCassDefaultRoadModel V0

 BASE Version for the Second Generation Default Road Network Model for Cassandra. 
 This repository contains the code for a purely C# based domain 
 model for Road Networks to run in Juno Cassandra. 
 
 This project should not be confused with the first version of the Cassandra 
 Default Road network model which is built using **JFunctions*. That (first 
 version) model relies purely on setup expressions for JFunctions and is 
 held in a different repository at fritzjoostenz account.
 
 This version, as last committed, gives a 100% agreement with the **JFunction Based**
 Default Road Network Model. Folder '*test_project_vs_jfunc*' contains a Cassandra
 Project which was used to compare the JFunction and C# models. A 100% comparison
 was achieved for a network with approximately 2,000 elements over a 30 year period.
 
 This project thus comprises the BASE version that compares directly to the JFunction
 based version. 
 
 Other versions consist of repositories first cloned from this one, and then 
 modified with improvements, simplifications as well as customisations for 
 specific clients/networks.
 
