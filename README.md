# Liðsvaldr
<a href="https://github.com/alex-ks/Lidsvaldr">
    <img src="http://ccfit.nsu.ru/~komissarov/Lidsvaldr.svg" width="40%" height="40%">
</a>

Workflow automation framework for .NET Core

```C#

IEnumerable<Picture> ProcessPhotos(IList<Picture> photos)
{
    var autoColor = new Func<Picture, Picture>(AutoColor).ToNode(threadLimit: 2);
    var edgeDetection = new Func<Picture, Picture>(DetectEdges).ToNode(threadLimit: 2);
    var mergePhotos = new Func<Picture, Picture, Picture>(MergePhotos).ToNode();

    autoColor.Input[0].AddCollection(photos);
    edgeDetection.Input[0].AddCollection(photos);
    mergePhotos.Input[0].Add(autoColor.Output[0]);
    mergePhotos.Input[1].Add(edgeDetection.Output[0]);
    
    var terminator = mergePhotos.Output[0].Terminate<Picture>();
    terminator.WaitForResults(photos.Count);
    
    return terminator.Results();
}
```

See [Wiki pages](https://github.com/alex-ks/Lidsvaldr/wiki) for details.
