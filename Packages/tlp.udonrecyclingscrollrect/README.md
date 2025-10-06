# Recycling Scroll Rect

> Note: this is not a polished release for VRChat!
>
> It works (vertical scrolling without grid) but is not user friendly in its current state.
> Please look at the demo scene for vertical scrolling (the one not marked as broken ;) )
---

https://twitter.com/polyandcode<br>
https://polyandcode.com || https://www.facebook.com/Polyandcode || https://www.instagram.com/polyandcode/

### Available [here](https://assetstore.unity.com/packages/tools/gui/Recycling-scroll-rect-178560) on the Unity Asset Store

 ## Summary
<b>UPDATE : Recycling Scroll Rect now supports Horizontal and Grid layout.</b><br><br>
Using the default Scroll-Rect to create lists with a huge number of items results in a laggy performance. Especially when creating a list with hundreds or thousands of items, it becomes impossible to use the Scroll Rect with the default approach i.e instantiating that many items. <i>Recycling Scroll Rect</i> reuses or recycles the least number of cells required to fill the viewport. As a result, a huge number of items can be shown in the list without any performance hit. Vertical, Horizontal and Grid layouts are supported. 



| Recycling  | Comparison with Default approach |
| ------------- | ------------- |
| ![Imgur Image](https://imgur.com/8LaILL7.gif)  | ![Imgur Image](https://imgur.com/pm9AmBH.gif) |

## Installation

**For simplicity reasons please install only via VRChat Creator companion and not manually using Unity packages! You will be missing dependencies when going the Unity package way!**

1. Import VRChat World SDK 3.6 VCC package into your project
2. Add CyanPlayerObjectPool package listing to VCC: https://cyanlaser.github.io/CyanPlayerObjectPool/
3. Add TLP Package listing to VCC: https://guribo.github.io/TLP/
4. Import TLP UdonRecyclingScrollRect into your project
5. *This will now automatically import all other needed packages for you:*
  - *TLP UdonUtils*
  - *CyanPlayerObjectPool*

## Quickstart Guide :

 <b>Check the Demo scenes for a complete example </b> <br><br>
 The usage and structure are similar to Native iOS <i>TableViewController</i>. There are mainly two parts in setting up a Recycling Scroll Rect; Prototype cell and DataSource. Following are the steps to set up a <i>Recycling Scroll Rect</i> in detail:
 
1. Recycling Scroll View
2. Prototype cell
3. Cell class
4. Datasource


<b>1. Recycling Scroll View</b>: You can create a <i>Recycling Scroll View</i> by going to <i>Create -> UI -> Recycling Scroll View</i>.
<img src="https://imgur.com/Bxjdvlw.gif" width="400" align="centre">
<br>

<b>2. Prototype Cell</b>: A Prototype cell is basically the cell layout for your list. A prototype cell can be in the hierarchy as the content's child or it can be a prefab. Don’t worry about disabling it if it is present in the hierarchy, it will not show up in play mode. The prototype cell must be assigned to the <i>Recycling Scroll Rect</i><br>
<img src="https://imgur.com/v5e4nG7.png" width="300" align="centre">


<b>3. Cell class</b>: Once you create your desired Prototype cell, assign it to the <i>Recycling Scroll Rect</i> component. Now you will need to create a <i>Cell</i> script and attach it to the Prototype Cell. This script must be a <i>Monobehaviour</i> implementing <i>ICell</i> interface. The purpose of a Cell script is to configure the cell as the list is scrolled or updated. You must keep reference to the UI items that are required to be updated according to your data source.
<b>Check <i>DemoCell</i> class for reference</b>
```c#
public class DemoCell : MonoBehaviour, ICell
{
    //UI
    public Text nameLabel;
    public Text genderLabel;
    public Text idLabel;

    //Model
    private ContactInfo _contactInfo;
    private int _cellIndex;

    //This is called from the SetCell method in DataSource
    public void ConfigureCell(ContactInfo contactInfo,int cellIndex)
    {
        _cellIndex = cellIndex;
        _contactInfo = contactInfo;

        nameLabel.text = contactInfo.Name;
        genderLabel.text = contactInfo.Gender;
        idLabel.text = contactInfo.id;
    }
}

```

<b> 4. Data source</b>: The next step is to create a Data source class. A Data source is responsible for data-related operations in the <i>Recycling Scroll Rect</i>. These are the number of items in the list and how a cell should be configured according to the data. To create a Data source, implement the <i>IRecyclingScrollRectDataSource</i> interface and its methods :<br>
  <b>• GetItemCount</b>: This method tells <i>Recycling Scroll Rect</i> the length of the List. <br>
  <b>• SetCell </b>: This method is responsible for configuring the cell UI according to your data. A cell is received as a parameter in this method with its index in the list. Using these, the necessary UI configuration can be done for the cell. The received cell is of <i>ICell</i> type. It must be cast to the implemented Cell type before using.<br><br>
  After the creation of a Cell and Data source, the last step is to assign the Data source instance to the <i>Recycling Scroll Rect</i>. The assignment must be done in <i>Awake</i> or before the <i>Recycling Scroll Rect</i>'s Start method (Check <i>others</i> section below for details on self-initialization).<br>
<b>Check <i>RecyclingScrollerDemo</i> class for reference</b>
```c#
public class RecyclingScrollerDemo : MonoBehaviour, IRecyclingScrollRectDataSource
{
    [SerializeField]
    RecyclingScrollRect _RecyclingScrollRect;

    [SerializeField]
    private int _dataLength;

    //Dummy data List
    private List<ContactInfo> _contactList = new List<ContactInfo>();

    //Recycling scroll rect's data source must be assigned in Awake.
    private void Awake()
    {
        InitData();
        _RecyclingScrollRect.DataSource = this;
    }

    #region DATA-SOURCE

    /// <summary>
    /// Data source method. return the list length.
    /// </summary>
    public int GetItemCount()
    {
        return _contactList.Count;
    }

    /// <summary>
    /// Called for a cell every time it is recycled
    /// Implement this method to do the necessary cell configuration.
    /// </summary>
    public void SetCell(ICell cell, int index)
    {
        //Casting to the implemented Cell
        var item = cell as DemoCell;
        item.ConfigureCell(_contactList[index],index);
    }
    
    #endregion
}
```
 
 <b> Others:</b> <br>
 <b>Self-Initiaze</b>: The <i>Recycling Scroll Rect</i> initializes on its own in its <i>Start</i> method. If you wish to initialize it yourself you can turn off the component's <i>self initialize</i> property and call the <i>Initialize</i> method whenever required. Make sure the Data-source is assigned before initializing.<br><br>
 <b>Reloading Data</b>: If a new data-source is asigned after initialization, call the <i>ReloadData()</i> function. Alternatively <i>ReloadData(IRecyclingScrollRectDataSource dataSource)</i> can also be used for assigning the data-source and reloading data.

### [0.1.0] - 2025-10-06

#### 🚀 Features

- Move from https://gitlab.com/vrc-archive/udon/tlp
- Start simplifying executionorder dependencies
- Add more events for different executionorder sections, refactor executionorder on most scripts
- Update tribes scene, create leaderboard prefab
- Update profiler ui, update leaderboard scene
- Reduce type spam in logs, add execution order to logs
- Create default factories for scenes, update logging
- Add comparer creation, update exectionorders, move pooleable code to base behaviour
- Add new data source using leaderboard model
- Add entries again, support ascending/descending sort direction (wip)
- Update after restructure
- Update UVU exporter and readme
- Update namespaces
- Update assets
- Update assembly definitions after vrc sdk updates
- Update pool references
- Update all
- Update

#### 🐛 Bug Fixes

- Bug in recycling
- Fix broken vertical scrolling demo world
- *(import)* Correct namespace path for MVC usage

#### 🚜 Refactor

- *(demo)* Remove unused `VRC.Udon` using directive

#### ⚡ Performance

- Reduce hitch on content refresh

#### ⚙️ Miscellaneous Tasks

- Add
- Update License
- Convert to LF
- Add test folder
- Fix issues
- Rename broken worlds
- Remove unused dependencies
