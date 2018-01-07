# Liðsvaldr
<a href="http://ccfit.nsu.ru/">
    <img src="http://ccfit.nsu.ru/~komissarov/Lidsvaldr.svg" width="40%" height="40%">
</a>

Workflow automation framework for .NET Core

```C#

IEnumerable<Picture> ProcessPhotos(IList<Picture> photos)
{
    var autoColor = new Func<Picture, Picture>(AutoColor).ToNode(threadLimit: 4);
    var mergePhotos = new Func<Picture, Picture, Picture>(MergePhotos).ToNode();

    autoColor.Input[0].Add(photos);
    mergePhotos.Input[0].Add(autoColor.Output[0]);
    mergePhotos.Input[1].Add(autoColor.Output[0]);
    
    var terminator = mergePhotos.Output[0].Terminate<Picture>();
    terminator.WaitForResults(photos.Count / 2);
    
    return terminator.Results();
}
```

See Wiki pages for details.
