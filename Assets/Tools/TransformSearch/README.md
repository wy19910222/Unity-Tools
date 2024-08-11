## TransformSearch

内含多种搜索，搜索结果以节点树的形式呈现。  
可继续扩展搜索条件。  
![SearchReferenceInScene](Captures~/SearchReferenceInScene.gif)  
![SearchComponent](Captures~/SearchComponent.gif)  
* **组件搜索：** 以选中目标为搜索范围（支持多选，支持Prefab编辑场景，支持Prefab资产），将范围内所有匹配的组件显示出来。  
    * 选中某个组件脚本，可以出现自动填入类名的按钮。  
    * 当以完整类名（Assembly.GetType返回不为null）搜索时，以Type对象进行搜索，搜索结果包括派生类对象。  
    * 当以非完整类名（有命名空间，却未带命名空间）搜索时，以类名字符串进行搜索，搜索结果可能包括不同命名空间的相同类名对象，但不包括派生类对象。  
* **Layer搜索：** 以选中目标为搜索范围（支持多选，支持Prefab编辑场景，支持Prefab资产），将范围内所有匹配的节点显示出来。  
* **场景中引用搜索：** 以整个场景为搜索范围（支持Prefab编辑场景），将范围内所有引用到所选对象（支持多选）的组件显示出来。  