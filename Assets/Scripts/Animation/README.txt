IKController: Custom script that is used to control IK targets / weights, for both
              the self-made IK system, and also for Animation Rigging IK Rig components

IKSystem:     Controls IK calculation and execution order for its given IKGroups.
              e.g, would be put on a character and would reference all its IKGroups.
              NOTE: IKGroups list will determine IK precedence. IKs in first group in the list
              will take precedence over those after it.

IKGroup:      Used to group a bunch of related IKElements that achieve some IK feature.
              e.g, an IKGroup could be made to store all the IKElements needed to produce
              an IK behaviour for running.
              "weight": The influence of this IKGroup in general

IKElement:    Base class for some IK behaviour.
              "IKObject": The transform it influences
              "weight": How strong the influence is
              "ofIKGroup": The IKGroup the IKElement belongs to.
              **AN IKELEMENT CAN ONLY BELONG TO ONE IKGROUP**


-- Components derived from IKElement:

IKRotationTargetAdditive: Used to aim a local axis of a transform at a given target.

              


TODO:
   IKRotationTargetAdditive should provide the actual direction the aim axis is facing
   rather than the current direction of its position to the target. Also dont make it rely
   on the IKTarget being some local thing, eventhough it should be some child of the 
   IKElement component object.

   Re-add animation rotation scaled to how much was lost due to weight.

ISSUE:
   Make one leg IK aim face sheer LEFT
   make another leg IK aim face sheer RIGHT 

   At eg LEFT weight = 1, RIGHT weight = 1
   legs still aimaing straight forward
   At LEFT weight = 0, RIGHT weight = 1
   legs aiming sheer RIGHT, accordingly
   At LEFT weight = 1, RIGHT weight = 0
   legs aiming straight???

   solution: just fucking add all the rotations * group weight for all IK's relating to a transform
