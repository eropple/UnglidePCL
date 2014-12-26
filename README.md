# Glide

Glide is a super-simple tweening library for C#.

**This is a Git and PCL/NuGet port of Jacob Albano's Glide. You can find the
original [here](https://bitbucket.org/jacobalbano/glide).**

## Installation
[Glide](https://www.nuget.org/packages/EdCanHack.Glide) is available
on NuGet. To install, run the following command in the Package Manager Console:

```
PM> Install-Package EdCanHack.Glide
```

## Use
Create a Tweener instance and use it to manage tweens:
    
```csharp
var Tweener = new Tweener();
Tweener.Tween(...);
```

Every frame, update the tweener.

```csharp
Tweener.Update(ElapsedSeconds);
```

### Tweening
Tweening properties is done with a call to Tween. Pass the object to tween, an [anonymous type][1] instance containing value names and target values, and a duration, with an optional delay.

```csharp
//	This tween will move the X and Y properties of the target
Tweener.Tween(target, new { X = toX, Y = toY }, duration, delay);
```
You can also use Glide to set up timed callbacks.

```csharp
Tweener.Timer(duration, delay).OnComplete(CompleteCallback);
```

### Control
If you need to control tweens after they are launched (for example, pausing or cancelling), you can hold a reference to the object returned By Tween():

```csharp
var myTween = Tweener.Tween(object, new {X = toX, Y = toY}, duration);
```

You can later use this object to control the tween:
    
```csharp
myTween.Cancel();
myTween.CancelAndComplete();

myTween.Pause();
myTween.PauseToggle();

myTween.Resume();
```

Calling a control method on a tweener will affect all tweens it manages.

If you'd rather not keep tween controller objects around, you can also control them by passing an object being tweened to a target control function.

```csharp

Tweener.Tween(myObject, ...);

Tweener.TargetCancel(myObject);
Tweener.TargetCancelAndComplete(myObject);

Tweener.TargetPause(myObject);
Tweener.TargetPauseToggle(myObject);

Tweener.TargetResume(myObject);
```

### Behavior
You can specify a number of special behaviors for a tween to use. Calls can be chained for setting more than one at a time.

```csharp
//  Glide comes with a full complement of easing functions
Tweener.Tween(...).Ease(Ease.ElasticOut);

Tweener.Tween(...).OnComplete(() => Console.WriteLine("done"));
Tweener.Tween(...).OnUpdate(() => Console.WriteLine("updating"));

//  Repeat twice
Tweener.Tween(...).Repeat(2);

//  Repeat forever
Tweener.Tween(...).Repeat();

//  Reverse the tween every other time it repeats
Tweener.Tween(...).Repeat().Reflect();

//  Swaps the end and start values of a tween.
//  This is helpful if you want to set an object's properties to one set of values, and then tween back to the previous values.
Tweener.Tween(...).Reverse();

//  Smoothly interpolate a rotation value past the end of an axis.
Tweener.Tween(...).Rotation();

//  Round tweened properties to integer values
Tweener.Tween(...).Round();
```

[1]: http://msdn.microsoft.com/en-us/library/vstudio/bb397696.aspx