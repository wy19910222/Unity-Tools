## ReferenceReplace

* 映射列表：保存替换前与替换后对象的映射关系。  
  * 原对象可以重复，但以排在列表前面的为准。  
  * 映射关系可以冗余，替换是若找不到则跳过。  
* 「清空列表」、「选中对象覆盖到左边」、「选中对象覆盖到右边」三个按钮：用于快捷操作列表。  
* 替换目标：可以是一个序列化为文本的资产，也可以是一个文件夹。  
* 「直接替换」按钮：直接对目标文件进行修改。  
* 「生成副本并替换」按钮：先生成目标文件的副本，然后对副本进行修改。  
  * 如果替换目标是一个序列化为文本的资产，则根据一定的命名规则新建副本，然后对副本进行修改。  
  * 如果替换目标是一个文件夹，则根据一定的命名规则新建一个文件夹，按对应路径为所有原目录下可操作的资产新建副本，然后对所有副本进行修改。  