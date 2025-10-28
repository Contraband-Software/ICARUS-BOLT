# ContraInventory

A bare-bones, EXTREMELY customizable inventory system framework.

## Usage

### Important Prerequisites

 - You MUST load the tags included in the `InventorySystemTagPreset.preset` preset file in the packages runtime directory, this adds the necessary Unity tags for the package to function.
 - ALL CONTAINERS MUST HAVE THE EXACT SAME ANCHORED POSITION WITHIN THE CONTAINERS-PARENT, OTHERWISE THE ITEM SNAPPING WILL BREAK.
 - References to the parent canvas must be added to the inventory manager.
 - ALL ITEMS AND SLOTS MUST HAVE A UNIQUE NAME, otherwise you will get red errors.
 - Only have ONE InventoryManager per scene, undefined behaviour otherwise.
 - ALL items MUST have a CanvasGroup component, otherwise input will not function.
 
 - Currently, mixing items/slots with and without custom defined behaviour is untested, do at your own risk

### Starting

Start by looking at the complete example setups in the `Samples > Prefabs` directory, add the canvas reference to the top-level InventoryManager and then you can see a full working example in play mode.

Right click on the `Canvas` in your project hierarchy, go to `UI > Cyan Inventory > Empty Manager`, This will add a set of GameObjects to get you started.

#### Remember

Every item container must have an `InventorySystemContainer` component.

Every item slot must have an `InventorySystemSlot` component.

Every item  must have an `InventorySystemItem` component (As well as a CanvasGroup).

#### Containers

Containers in this framework refers to a collection of items stored together, like a chest or player inventory. You can have multiple different containers representing different, optionally isolated, inventories.

All of these containers must reside under the `Containers` GameObject under the InventoryManager; It can be a bit confusing as it is a container container!

Right click on the `Containers` container object under the InventorySystem you just created, navigate to `UI > Cyan Inventory > Container`. Give it a nice **UNIQUE** name.

Do not reposition this container if you want to have mutliple containers, it's anchored position must be the same as all other containers in the scene. You must only work with the position of the child item slots.

Containers can be isolated, meaning items from other containers cannot be transferred to it, nor can items from it be transferred to other containers. You can also specify container groups in which items can be transferred. Click a container you have created, under isolation settings, you can enable them and then give it a group string that gorups it with other containers that have that exact same string.

<p>
    <img src="/Documentation~/README/Isolation.png"/>
</p>

#### Slots

Slots are individual spaces for items within a container.

Right click on the individual container you just created, navigate to `UI > Cyan Inventory > Slots > Basic Slot`. Give it a nice **UNIQUE** name. Position it wherever you want.

#### Items

Right click on the `Items` container object under the InventorySystem you just created, navigate to `UI > Cyan Inventory > Items > Basic Item`. Give it a nice **UNIQUE** name. Its position does not matter.

#### Setting items to start out in slots

Click the top-level inventory manager, you will see an `InventorySystemInitializer` component on it. Expand the list by one, add the item and then add the slot you want it to start in.

Now you have an inventory! But currently it only has one item and one slot, so get creative!

### Custom behaviour and rules

All logic for whether an item can enter a slot is done within the slot component, so, add a new script to a slot, make it inherit from the abstract class `cyanseraph.InventorySystem.InventorySystemSlotBehaviour`.

You will now implement the method `public bool CanItemSlot(GameObject item);` on your script, otherwise it will not compile.

How you define this method is entirely up to you now - you are given the GameObject of the item trying to slot, so you can access your own scripts storing type information on them; the way I will do it is with a public enum defining *item type*, an item will have an item type and a slot will have an accepted item type. (In the image below I have also added some logic to change some child text on the item slot to its type)

Go back to your slot gameobject, under the `Custom Slot Behaviour` drop down, click `Enable`, add the reference to your slot behaviour script (which should be on the same GameObject). Now you must make your items store their type somehow and access it in the `CanItemSlot` function.

<p>
    <img src="/Documentation~/README/SlotBehaviour.png"/>
</p>
<p>
    <img src="/Documentation~/README/SlotBehaviour1.png"/>
</p>
<p>
    <img src="/Documentation~/README/SlotBehaviour2.png"/>
</p>

Now items of type A can only slot in slots with type A. I recommend making a slot and item prefab storing your setup.

### Programatic access to the inventory, its containers, and slots

The inventory manager component on the top-level inventory system gameobject exposes many methods for accessing containers and gettting lists of items, spawning items in specific slots, etc.

## Best practices

 - The inventory manager should only contain a "containers" container GameObject and an "items" container GameObject, each of these GameObjects should also only contain their respective GameObjects - You run the risk of causing breakages if you do not follow this.

## Limitations

 - Currently does not support stackable items.
 - All active items MUST exist in inventory slots, this means you cannot have items lying on the ground for example. This can be mitigated with invisible item slots.
