#Set-SPOWikiPageContent
Sets the contents of a wikipage
##Syntax
```powershell
Set-SPOWikiPageContent -Path <String> -ServerRelativePageUrl <String> [-Web <WebPipeBind>]
```


```powershell
Set-SPOWikiPageContent -Content <String> -ServerRelativePageUrl <String> [-Web <WebPipeBind>]
```


##Parameters
Parameter|Type|Required|Description
---------|----|--------|-----------
|Content|String|True||
|Path|String|True||
|ServerRelativePageUrl|String|True|Site Relative Page Url|
|Web|WebPipeBind|False|The web to apply the command to. Omit this parameter to use the current web.|
