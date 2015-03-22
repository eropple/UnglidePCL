using System;
using System.Collections.Generic;
using System.Reflection;

namespace Unglide
{
    public partial class Tween
    {
        [Flags]
        private enum Behavior
        {
            None,
            Reflect,
            Rotation,
            Round
        }

        #region Callbacks
        private Func<float, float> _ease;
        private Action _begin, _complete;
        private Action<float> _update;
        #endregion

        #region Timing
        public bool Paused { get; private set; }
        protected float Delay;
        protected float Duration;

        private float _time;
        private float _elapsed;
        #endregion

        private int _repeatCount;
        private Behavior _behavior;

        private readonly List<float> _start;
        private readonly List<float> _range;
        private readonly List<float> _end;
        private readonly List<UnglideInfo> _vars;

        protected object Target;
        private TweenerImpl _parent;

        public float TimeRemaining => Duration - _time;
        public float Completion { get { var c = _time / Duration; return c < 0 ? 0 : (c > 1 ? 1 : c); } }

        public bool Looping => _repeatCount > 0;

        public Tween()
        {
            _elapsed = 0;

            _vars = new List<UnglideInfo>();
            _start = new List<float>();
            _range = new List<float>();
            _end = new List<float>();
        }

        internal void Update()
        {
            if (Paused)
                return;

            if (Delay > 0)
            {
                Delay -= _elapsed;
                return;
            }

            if (Math.Abs(_time) < Single.Epsilon)
            {
                _begin?.Invoke();
            }

            _update?.Invoke(Completion);

            _time += _elapsed;
            float t = _time / Duration;
            bool doComplete = false;

            if (_time >= Duration)
            {
                if (_repeatCount > 0)
                {
                    --_repeatCount;
                    _time = t = 0;
                }
                else if (_repeatCount < 0)
                {
                    doComplete = true;
                    _time = t = 0;
                }
                else
                {
                    _time = Duration;
                    t = 1;
                    _parent.Remove(this);
                    doComplete = true;
                }

                if (Math.Abs(_time) < Single.Epsilon)
                {
                    //	If the timer is zero here, we just restarted.
                    //	If reflect mode is on, flip start to end
                    if ((_behavior & Behavior.Reflect) == Behavior.Reflect)
                        Reverse();
                }
            }

            if (_ease != null)
                t = _ease(t);

            Interpolate(t);

            if (doComplete && _complete != null)
                _complete();
        }

        protected virtual void Interpolate(float t)
        {
            int i = _vars.Count;
            while (i-- > 0)
            {
                float value = _start[i] + _range[i] * t;
                if ((_behavior & Behavior.Round) == Behavior.Round)
                {
                    value = (float)Math.Round(value);
                }

                if ((_behavior & Behavior.Rotation) == Behavior.Rotation)
                {
                    float angle = value % 360.0f;

                    if (angle < 0)
                    {
                        angle += 360.0f;
                    }

                    value = angle;
                }

                _vars[i].Value = value;
            }
        }

        #region Behavior

        /// <summary>
        /// Apply target values to a starting point before tweening.
        /// </summary>
        /// <param name="values">The values to apply, in an anonymous type ( new { prop1 = 100, prop2 = 0} ).</param>
        /// <returns>A reference to this.</returns>
        public Tween From(object values)
        {
            foreach (var property in values.GetType().GetTypeInfo().DeclaredProperties)
            {
                var property1 = property;
                var index = _vars.FindIndex(i => String.Compare(i.Name, property1.Name, StringComparison.OrdinalIgnoreCase) == 0);
                if (index >= 0)
                {
                    //	if we're already tweening this value, adjust the range
                    var info = _vars[index];

                    var to = new UnglideInfo(values, property.Name, false);
                    info.Value = to.Value;

                    _start[index] = Convert.ToSingle(info.Value);
                    _range[index] = _end[index] - _start[index];
                }
                else
                {
                    //	if we aren't tweening this value, just set it
                    var info = new UnglideInfo(Target, property.Name);
                    var to = new UnglideInfo(values, property.Name, false);
                    info.Value = to.Value;
                }
            }

            return this;
        }

        /// <summary>
        /// Set the easing function.
        /// </summary>
        /// <param name="ease">The Easer to use.</param>
        /// <returns>A reference to this.</returns>
        public Tween Ease(Func<float, float> ease)
        {
            _ease = ease;
            return this;
        }

        /// <summary>
        /// Set a function to call when the tween begins (useful when using delays).
        /// </summary>
        /// <param name="callback">The function that will be called when the tween starts, after the delay.</param>
        /// <returns>A reference to this.</returns>
        public Tween OnBegin(Action callback)
        {
            _begin = callback;
            return this;
        }

        /// <summary>
        /// Set a function to call when the tween finishes.
        /// If the tween repeats infinitely, this will be called each time; otherwise it will only run when the tween is finished repeating.
        /// </summary>
        /// <param name="callback">The function that will be called on tween completion.</param>
        /// <returns>A reference to this.</returns>
        public Tween OnComplete(Action callback)
        {
            _complete = callback;
            return this;
        }

        /// <summary>
        /// Set a function to call as the tween updates.
        /// </summary>
        /// <param name="callback">The function to use.</param>
        /// <returns>A reference to this.</returns>
        public Tween OnUpdate(Action<Single> callback)
        {
            _update = callback;
            return this;
        }

        /// <summary>
        /// Enable repeating.
        /// </summary>
        /// <param name="times">Number of times to repeat. Leave blank or pass a negative number to repeat infinitely.</param>
        /// <returns>A reference to this.</returns>
        public Tween Repeat(int times = -1)
        {
            _repeatCount = times;
            return this;
        }

        /// <summary>
        /// Sets the tween to reverse every other time it repeats. Repeating must be enabled for this to have any effect.
        /// </summary>
        /// <returns>A reference to this.</returns>
        public Tween Reflect()
        {
            _behavior |= Behavior.Reflect;
            return this;
        }

        /// <summary>
        /// Swaps the start and end values of the tween.
        /// </summary>
        /// <returns>A reference to this.</returns>
        public virtual Tween Reverse()
        {
            int count = _vars.Count;
            while (count-- > 0)
            {
                float s = _start[count];
                float r = _range[count];

                //	Set start to end and end to start
                _start[count] = s + r;
                _range[count] = s - (s + r);
            }

            return this;
        }

        /// <summary>
        /// Whether this tween handles rotation.
        /// </summary>
        /// <returns>A reference to this.</returns>
        public Tween Rotation()
        {
            _behavior |= Behavior.Rotation;

            int count = _vars.Count;
            while (count-- > 0)
            {
                float angle = _start[count];
                float r = angle + _range[count];

                float d = r - angle;
                float a = Math.Abs(d);

                if (a > 181)
                {
                    r = (360 - a) * (d > 0 ? -1 : 1);
                }
                else if (a < 179)
                {
                    r = d;
                }
                else
                {
                    r = 180;
                }

                _range[count] = r;
            }

            return this;
        }

        /// <summary>
        /// Whether tweened values should be rounded to integer values.
        /// </summary>
        /// <returns>A reference to this.</returns>
        public Tween Round()
        {
            _behavior |= Behavior.Round;
            return this;
        }
        #endregion

        #region Control
        /// <summary>
        /// Remove tweens from the tweener without calling their complete functions.
        /// </summary>
        public void Cancel()
        {
            _parent.Remove(this);
        }

        /// <summary>
        /// Assign tweens their final value and remove them from the tweener.
        /// </summary>
        public void CancelAndComplete()
        {
            _time = Duration;
            _update = null;
            _parent.Remove(this);
        }

        /// <summary>
        /// Set tweens to pause. They won't update and their delays won't tick down.
        /// </summary>
        public void Pause()
        {
            Paused = true;
        }

        /// <summary>
        /// Toggle tweens' paused value.
        /// </summary>
        public void PauseToggle()
        {
            Paused = !Paused;
        }

        /// <summary>
        /// Resumes tweens from a paused state.
        /// </summary>
        public void Resume()
        {
            Paused = false;
        }
        #endregion
    }
}