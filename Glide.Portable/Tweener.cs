using System;
using System.Collections.Generic;
using System.Reflection;

namespace Glide
{
    public class Tweener : Tween.TweenerImpl
    { }

    public partial class Tween
    {
        public class TweenerImpl
        {
            static TweenerImpl()
            {
                _dummy = new { };
                Tweener = new Tweener();
            }

            public TweenerImpl()
            {
                tweens = new Dictionary<object, List<Tween>>();
                toRemove = new List<Tween>();
                toAdd = new List<Tween>();
            }

            public static readonly Tweener Tweener;
            private static object _dummy;
            private Dictionary<object, List<Tween>> tweens;
            private List<Tween> toRemove, toAdd;

            /// <summary>
            /// Tweens a set of numeric properties on an object.
            /// To tween instance properties/fields, pass the object.
            /// To tween static properties/fields, pass the type of the object, using typeof(ObjectType) or object.GetType().
            /// </summary>
            /// <param name="target">The object or type to tween.</param>
            /// <param name="values">The values to tween to, in an anonymous type ( new { prop1 = 100, prop2 = 0} ).</param>
            /// <param name="duration">Duration of the tween in seconds.</param>
            /// <param name="delay">Delay before the tween starts, in seconds.</param>
            /// <returns>The tween created, for setting properties on.</returns>
            public Tween Tween<T>(T target, object values, float duration, float delay = 0) where T : class
            {
                var targetInfo = target.GetType().GetTypeInfo();
                if (targetInfo.IsValueType)
                    throw new Exception("Target of tween cannot be a struct!");

                var tween = new Tween();

                tween.Target = target;
                tween.Duration = duration;
                tween.Delay = delay;

                AddTween(tween);

                if (values == null) // in case of timer
                    return tween;

                foreach (PropertyInfo property in values.GetType().GetTypeInfo().DeclaredProperties)
                {
                    var info = new GlideInfo(target, property.Name);
                    var to = Convert.ToSingle(new GlideInfo(values, property.Name, false).Value);

                    float s = Convert.ToSingle(info.Value);
                    float r = to - s;

                    tween._vars.Add(info);
                    tween._start.Add(s);
                    tween._range.Add(r);
                    tween._end.Add(to);
                }

                return tween;
            }

            /// <summary>
            /// Manually add a tween to the tweener.
            /// Only use this to add custom tween classes!
            /// </summary>
            /// <param name="tween">The tween to add.</param>
            public void AddTween(Tween tween)
            {
                tween._parent = this;
                toAdd.Add(tween);
            }

            /// <summary>
            /// Starts a simple timer for setting up callback scheduling.
            /// </summary>
            /// <param name="duration">How long the timer will run for, in seconds.</param>
            /// <param name="delay">How long to wait before starting the timer, in seconds.</param>
            /// <returns>The tween created, for setting properties.</returns>
            public Tween Timer(float duration, float delay = 0)
            {
                return Tween(_dummy, null, duration, delay);
            }

            /// <summary>
            /// Remove tweens from the tweener without calling their complete functions.
            /// </summary>
            public void Cancel()
            {
                ApplyAll(glide => toRemove.Add(glide));
            }

            /// <summary>
            /// Assign tweens their final value and remove them from the tweener.
            /// </summary>
            public void CancelAndComplete()
            {
                ApplyAll(glide =>
                {
                    glide._time = glide.Duration;
                    glide._update = null;
                    toRemove.Add(glide);
                });
            }

            /// <summary>
            /// Set tweens to pause. They won't update and their delays won't tick down.
            /// </summary>
            public void Pause()
            {
                ApplyAll(glide => glide.Paused = true);
            }

            /// <summary>
            /// Toggle tweens' paused value.
            /// </summary>
            public void PauseToggle()
            {
                ApplyAll(glide => glide.Paused = !glide.Paused);
            }

            /// <summary>
            /// Resumes tweens from a paused state.
            /// </summary>
            public void Resume()
            {
                ApplyAll(glide => glide.Paused = false);
            }

            /// <summary>
            /// Updates the tweener and all objects it contains.
            /// </summary>
            /// <param name="secondsElapsed">Seconds elapsed since last update.</param>
            public void Update(float secondsElapsed)
            {
                ApplyAll(glide =>
                {
                    glide._elapsed = secondsElapsed;
                    glide.Update();
                });

                AddAndRemove();
            }

            internal void Remove(Tween glide)
            {
                toRemove.Add(glide);
            }

            private void ApplyAll(Action<Tween> action)
            {
                foreach (var list in tweens.Values)
                {
                    foreach (var glide in list)
                    {
                        action(glide);
                    }
                }
            }

            private void AddAndRemove()
            {
                foreach (var add in toAdd)
                {
                    List<Tween> list = null;
                    if (!tweens.TryGetValue(add.Target, out list))
                        tweens[add.Target] = list = new List<Tween>();

                    list.Add(add);
                }

                foreach (var remove in toRemove)
                {
                    List<Tween> list;
                    if (tweens.TryGetValue(remove.Target, out list))
                    {
                        list.Remove(remove);
                        if (list.Count == 0)
                        {
                            tweens.Remove(remove.Target);
                        }
                    }
                }

                toAdd.Clear();
                toRemove.Clear();
            }

            #region Bulk control

            private void ApplyBulkControl(object[] targets, Action<Tween> action)
            {
                foreach (var target in targets)
                {
                    List<Tween> list;
                    if (tweens.TryGetValue(target, out list))
                    {
                        foreach (var glide in list)
                        {
                            action(glide);
                        }
                    }
                }
            }

            /// <summary>
            /// Look up tweens by the objects they target, and cancel them.
            /// </summary>
            /// <param name="targets">The objects being tweened that you want to cancel.</param>
            public void TargetCancel(params object[] targets)
            {
                ApplyBulkControl(targets, glide => glide.Cancel());
            }

            /// <summary>
            /// Look up tweens by the objects they target, cancel them, set them to their final values, and call the complete callback.
            /// </summary>
            /// <param name="targets">The objects being tweened that you want to cancel and complete.</param>
            public void TargetCancelAndComplete(params object[] targets)
            {
                ApplyBulkControl(targets, glide => glide.CancelAndComplete());
            }


            /// <summary>
            /// Look up tweens by the objects they target, and pause them.
            /// </summary>
            /// <param name="targets">The objects being tweened that you want to pause.</param>
            public void TargetPause(params object[] targets)
            {
                ApplyBulkControl(targets, glide => glide.Pause());
            }

            /// <summary>
            /// Look up tweens by the objects they target, and toggle their paused states.
            /// </summary>
            /// <param name="targets">The objects being tweened that you want to toggle pause.</param>
            public void TargetPauseToggle(params object[] targets)
            {
                ApplyBulkControl(targets, glide => glide.PauseToggle());
            }


            /// <summary>
            /// Look up tweens by the objects they target, and resume them from paused.
            /// </summary>
            /// <param name="targets">The objects being tweened that you want to resume.</param>
            public void TargetResume(params object[] targets)
            {
                ApplyBulkControl(targets, glide => glide.Resume());
            }

            #endregion
        }
    }
}
