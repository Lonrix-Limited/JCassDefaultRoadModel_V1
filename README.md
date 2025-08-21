# JCassDefaultRoadModel V1

 Updated Version for the Second Generation Default Road Network Model for Cassandra. 
 This version is a clone of the BASE model (V0) repository. The BASE C# model 
 was largely aimed at getting a starting point that matches with the JFunction model.

 Bacause the BASE (V0) model was closely aligned with the JFunction model logic, there 
 was some complexity in the logic, which was not always necessary for a C# implementation.

 This model (V1) is a clone of the BASE model, but with some improvements to
 simplify some complex logic and get better consistency in coding convetions.

 ## List of Key Changes

 ### S-Curve Setup

 The setup of S-curve parameters from lookup is now more structured and flexible. It is possible to
 assign differrent AADI and T100 limits for each distress type. There is a default type, but with small
 tweaks in the code, separate limits can be assigned for each distress type. Currently, the default type covers
 all distresses except for Potholes.

 ### S-Curve Resets

 The reset of S-curve parameters previously relied on quite complext logic. Now, the reset logic is split for
 Rehabs (full reset), Resurfacing (partial reset if distress is above a certain threshold), and Holding Actions
 (partial reset if distress is above a certain threshold, not the same as for Resurfacing).

 Previous model applied another reset for second-coats over Holding Actions. This is wrong, because the distress
 will have been already reset by the Holding Action, so when the second-coat is applied, the distress is zero leading
 to a full reset which may not be warranted if the Preseal is done over a large distress area. New implementation will
 not reset the S-curve parameters for second-coats over Holding Actions, but will simply continue with the S-curve
 parameters from the Holding Action (which would have taken into account the distress area when it was reset).

 ### Thin AC Overlays 
 
 With the latest version of the Cassandra Framework model, the cost of a treatment can be split into more than
 one Budget category. Using this feature, for Thin AC we now use the distress area to automatically calculate the
 area to be repaired before overlay. This cost is assigned to the 'Preseal Repairs' budget. The overlay cost is then
 assigned to the "Resurfacing" budget. This can be changed if the clients to not have a 'Preseal Repairs' budget. The
 treatment cost is then the total cost of the repairs and the overlay.

 The above composite treatment is currently only considred ion the MCDA model, and only if the percentage of Surface 
 Life Achieved is above a certain percentage (specified in the lookups).

 ### AC Heavy Maintenance Treatment Added

 Based on feedback from the clients, we see a need for a Heavy Maintenance treatment for AC pavements that will fix existing
 distress but does not include a full overlay. This treatment is considered in the MCDA model if the Achieved Surface Life is 
 below above a certain percentage (specified in the lookups).


 
