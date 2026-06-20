/***********************
 * 代码修改自 avalonia (https://github.com/AvaloniaUI/Avalonia)
 * license: MIT
 ***********************/

using System.Collections;
using System.Diagnostics;

namespace NewBeeVG.Internal;

/// <summary>
/// DefinitionBase provides core functionality used internally by Grid
/// and ColumnDefinitionCollection / RowDefinitionCollection
/// </summary>
public abstract class NBDefinitionBase
{
    /// <summary>
    /// SharedSizeGroup property.
    /// </summary>
    public string? SharedSizeGroup { get; set; }

    /// <summary>
    /// Performs action preparing definition to enter layout calculation mode.
    /// </summary>
    internal void OnBeforeLayout(NBGrid grid)
    {
        //  reset layout state.
        _minSize = 0;
        LayoutWasUpdated = true;

        //  defer verification for shared definitions
        _sharedState?.EnsureDeferredValidation(grid);
    }

    /// <summary>
    /// Updates min size.
    /// </summary>
    /// <param name="minSize">New size.</param>
    internal void UpdateMinSize(double minSize)
    {
        _minSize = Math.Max(_minSize, minSize);
    }

    /// <summary>
    /// Sets min size.
    /// </summary>
    /// <param name="minSize">New size.</param>
    internal void SetMinSize(double minSize)
    {
        _minSize = minSize;
    }

    /// <summary>
    /// Returns <c>true</c> if this definition is a part of shared group.
    /// </summary>
    internal bool IsShared
    {
        get => (_sharedState != null);
    }

    /// <summary>
    /// Internal accessor to user size field.
    /// </summary>
    internal GridLength UserSize
    {
        get => (_sharedState != null ? _sharedState.UserSize : UserSizeValueCache);
    }

    /// <summary>
    /// Internal accessor to user min size field.
    /// </summary>
    internal double UserMinSize
    {
        get => (UserMinSizeValueCache);
    }

    /// <summary>
    /// Internal accessor to user max size field.
    /// </summary>
    internal double UserMaxSize
    {
        get => (UserMaxSizeValueCache);
    }

    /// <summary>
    /// DefinitionBase's index in the parents collection.
    /// </summary>
    internal int Index
    {
        get => (_parentIndex);
        set
        {
            Debug.Assert(value >= -1);
            _parentIndex = value;
        }
    }

    /// <summary>
    /// Layout-time user size type.
    /// </summary>
    internal NBGrid.LayoutTimeSizeType SizeType
    {
        get => (_sizeType);
        set => _sizeType = value;
    }

    /// <summary>
    /// Returns or sets measure size for the definition.
    /// </summary>
    internal double MeasureSize
    {
        get => (_measureSize);
        set => _measureSize = value;
    }

    /// <summary>
    /// Returns definition's layout time type sensitive preferred size.
    /// </summary>
    /// <remarks>
    /// Returned value is guaranteed to be true preferred size.
    /// </remarks>
    internal double PreferredSize
    {
        get
        {
            double preferredSize = MinSize;
            if (_sizeType != NBGrid.LayoutTimeSizeType.Auto
                && preferredSize < _measureSize)
            {
                preferredSize = _measureSize;
            }
            return (preferredSize);
        }
    }

    /// <summary>
    /// Returns or sets size cache for the definition.
    /// </summary>
    internal double SizeCache
    {
        get => (_sizeCache);
        set => _sizeCache = value;
    }

    /// <summary>
    /// Returns min size.
    /// </summary>
    internal double MinSize
    {
        get
        {
            double minSize = _minSize;
            if (UseSharedMinimum
                && _sharedState != null
                && minSize < _sharedState.MinSize)
            {
                minSize = _sharedState.MinSize;
            }
            return (minSize);
        }
    }

    /// <summary>
    /// Returns min size, always taking into account shared state.
    /// </summary>
    internal double MinSizeForArrange
    {
        get
        {
            double minSize = _minSize;
            if (_sharedState != null
                && (UseSharedMinimum || !LayoutWasUpdated)
                && minSize < _sharedState.MinSize)
            {
                minSize = _sharedState.MinSize;
            }
            return (minSize);
        }
    }

    /// <summary>
    /// Offset.
    /// </summary>
    internal double FinalOffset
    {
        get => _offset;
        set => _offset = value;
    }

    /// <summary>
    /// Internal helper to access up-to-date UserSize property value.
    /// </summary>
    internal abstract GridLength UserSizeValueCache { get; }

    /// <summary>
    /// Internal helper to access up-to-date UserMinSize property value.
    /// </summary>
    internal abstract double UserMinSizeValueCache { get; }

    /// <summary>
    /// Internal helper to access up-to-date UserMaxSize property value.
    /// </summary>
    internal abstract double UserMaxSizeValueCache { get; }

    internal NBGrid? Parent { get; set; }

    /// <summary>
    /// SetFlags is used to set or unset one or multiple
    /// flags on the object.
    /// </summary>
    private void SetFlags(bool value, Flags flags)
    {
        _flags = value ? (_flags | flags) : (_flags & (~flags));
    }

    /// <summary>
    /// CheckFlagsAnd returns <c>true</c> if all the flags in the
    /// given bitmask are set on the object.
    /// </summary>
    private bool CheckFlagsAnd(Flags flags)
    {
        return ((_flags & flags) == flags);
    }

    /// <remarks>
    /// Verifies that Shared Size Group Property string
    /// a) not empty.
    /// b) contains only letters, digits and underscore ('_').
    /// c) does not start with a digit.
    /// </remarks>
    private static bool SharedSizeGroupPropertyValueValid(string? id)
    {
        //  null is default value
        if (id == null)
        {
            return true;
        }

        if (id.Length > 0)
        {
            int i = -1;
            while (++i < id.Length)
            {
                bool isDigit = Char.IsDigit(id[i]);

                if ((i == 0 && isDigit)
                    || !(isDigit
                        || Char.IsLetter(id[i])
                        || '_' == id[i]))
                {
                    break;
                }
            }

            if (i == id.Length)
            {
                return true;
            }
        }

        return false;
    }


    /// <summary>
    /// Convenience accessor to UseSharedMinimum flag
    /// </summary>
    private bool UseSharedMinimum { get; set; }

    /// <summary>
    /// Convenience accessor to LayoutWasUpdated flag
    /// </summary>
    private bool LayoutWasUpdated
    {
        get => (CheckFlagsAnd(Flags.LayoutWasUpdated));
        set => SetFlags(value, Flags.LayoutWasUpdated);
    }

    private Flags _flags;                           //  flags reflecting various aspects of internal state
    internal int _parentIndex = -1;                  //  this instance's index in parent's children collection

    private NBGrid.LayoutTimeSizeType _sizeType;      //  layout-time user size type. it may differ from _userSizeValueCache.UnitType when calculating "to-content"

    private double _minSize;                        //  used during measure to accumulate size for "Auto" and "Star" DefinitionBase's
    private double _measureSize;                    //  size, calculated to be the input constraint size for Child.Measure
    private double _sizeCache;                      //  cache used for various purposes (sorting, caching, etc) during calculations
    private double _offset;                         //  offset of the DefinitionBase from left / top corner (assuming LTR case)

    private SharedSizeState? _sharedState;           //  reference to shared state object this instance is registered with

    [Flags]
    private enum Flags : byte
    {
        //
        //  bool flags
        //
        UseSharedMinimum = 0x00000020,     //  when "1", definition will take into account shared state's minimum
        LayoutWasUpdated = 0x00000040,     //  set to "1" every time the parent grid is measured
    }

    /// <summary>
    /// Collection of shared states objects for a single scope
    /// </summary>
    internal class SharedSizeScope
    {
        /// <summary>
        /// Returns SharedSizeState object for a given group.
        /// Creates a new StatedState object if necessary.
        /// </summary>
        internal SharedSizeState EnsureSharedState(string sharedSizeGroup)
        {
            //  check that sharedSizeGroup is not default
            Debug.Assert(sharedSizeGroup != null);

            SharedSizeState? sharedState = _registry[sharedSizeGroup] as SharedSizeState;
            if (sharedState == null)
            {
                sharedState = new SharedSizeState(this, sharedSizeGroup);
                _registry[sharedSizeGroup] = sharedState;
            }
            return (sharedState);
        }

        /// <summary>
        /// Removes an entry in the registry by the given key.
        /// </summary>
        internal void Remove(object key)
        {
            Debug.Assert(_registry.Contains(key));
            _registry.Remove(key);
        }

        private Hashtable _registry = new Hashtable();  //  storage for shared state objects
    }

    /// <summary>
    /// Implementation of per shared group state object
    /// </summary>
    internal class SharedSizeState
    {
        /// <summary>
        /// Default ctor.
        /// </summary>
        internal SharedSizeState(SharedSizeScope sharedSizeScope, string sharedSizeGroupId)
        {
            _sharedSizeScope = sharedSizeScope;
            _sharedSizeGroupId = sharedSizeGroupId;
            _registry = new List<NBDefinitionBase>();
            _layoutUpdated = OnLayoutUpdated;
            _broadcastInvalidation = true;
        }

        /// <summary>
        /// Adds / registers a definition instance.
        /// </summary>
        internal void AddMember(NBDefinitionBase member)
        {
            Debug.Assert(!_registry.Contains(member));
            _registry.Add(member);
            Invalidate();
        }

        /// <summary>
        /// Removes / un-registers a definition instance.
        /// </summary>
        /// <remarks>
        /// If the collection of registered definitions becomes empty
        /// instantiates self removal from owner's collection.
        /// </remarks>
        internal void RemoveMember(NBDefinitionBase member)
        {
            Invalidate();
            _registry.Remove(member);

            if (_registry.Count == 0)
            {
                _sharedSizeScope.Remove(_sharedSizeGroupId);
            }
        }

        /// <summary>
        /// Propagates invalidations for all registered definitions.
        /// Resets its own state.
        /// </summary>
        internal void Invalidate()
        {
            _userSizeValid = false;
        }

        /// <summary>
        /// Makes sure that one and only one layout updated handler is registered for this shared state.
        /// </summary>
        internal void EnsureDeferredValidation(NBVisual layoutUpdatedHost)
        {
            if (_layoutUpdatedHost == null)
            {
                _layoutUpdatedHost = layoutUpdatedHost;
            }
        }

        /// <summary>
        /// DefinitionBase's specific code.
        /// </summary>
        internal double MinSize
        {
            get
            {
                if (!_userSizeValid) { EnsureUserSizeValid(); }
                return (_minSize);
            }
        }

        /// <summary>
        /// DefinitionBase's specific code.
        /// </summary>
        internal GridLength UserSize
        {
            get
            {
                if (!_userSizeValid) { EnsureUserSizeValid(); }
                return (_userSize);
            }
        }

        private void EnsureUserSizeValid()
        {
            _userSize = new GridLength(1, GridUnitType.Auto);

            for (int i = 0, count = _registry.Count; i < count; ++i)
            {
                Debug.Assert(_userSize.GridUnitType == GridUnitType.Auto
                            || _userSize.GridUnitType == GridUnitType.Pixel);

                GridLength currentGridLength = _registry[i].UserSizeValueCache;
                if (currentGridLength.GridUnitType == GridUnitType.Pixel)
                {
                    if (_userSize.GridUnitType == GridUnitType.Auto)
                    {
                        _userSize = currentGridLength;
                    }
                    else if (_userSize.Value < currentGridLength.Value)
                    {
                        _userSize = currentGridLength;
                    }
                }
            }
            //  taking maximum with user size effectively prevents squishy-ness.
            //  this is a "solution" to avoid shared definitions from been sized to
            //  different final size at arrange time, if / when different grids receive
            //  different final sizes.
            _minSize = _userSize.IsAbsolute ? _userSize.Value : 0.0;

            _userSizeValid = true;
        }

        /// <summary>
        /// OnLayoutUpdated handler. Validates that all participating definitions
        /// have updated min size value. Forces another layout update cycle if needed.
        /// </summary>
        private void OnLayoutUpdated(object? sender, EventArgs e)
        {
            double sharedMinSize = 0;

            //  accumulate min size of all participating definitions
            for (int i = 0, count = _registry.Count; i < count; ++i)
            {
                sharedMinSize = Math.Max(sharedMinSize, _registry[i].MinSize);
            }

            bool sharedMinSizeChanged = !MathUtilities.AreClose(_minSize, sharedMinSize);

            //  compare accumulated min size with min sizes of the individual definitions
            for (int i = 0, count = _registry.Count; i < count; ++i)
            {
                NBDefinitionBase definitionBase = _registry[i];

                // we'll set d.UseSharedMinimum to maintain the invariant:
                //      d.UseSharedMinimum iff d._minSize < this.MinSize
                // i.e. iff d is not a "long-pole" definition.
                //
                // Measure/Arrange of d's Grid uses d._minSize for long-pole
                // definitions, and max(d._minSize, shared size) for
                // short-pole definitions.  This distinction allows us to react
                // to changes in "long-pole-ness" more efficiently and correctly,
                // by avoiding remeasures when a long-pole definition changes.
                bool useSharedMinimum = !MathUtilities.AreClose(definitionBase._minSize, sharedMinSize);

                // before doing that, determine whether d's Grid needs to be remeasured.
                // It's important _not_ to remeasure if the last measure is still
                // valid, otherwise infinite loops are possible
                bool measureIsValid;

                if (!definitionBase.UseSharedMinimum)
                {
                    // d was a long-pole.  measure is valid iff it's still a long-pole,
                    // since previous measure didn't use shared size.
                    measureIsValid = !useSharedMinimum;
                }
                else if (useSharedMinimum)
                {
                    // d was a short-pole, and still is.  measure is valid
                    // iff the shared size didn't change
                    measureIsValid = !sharedMinSizeChanged;
                }
                else
                {
                    // d was a short-pole, but is now a long-pole.  This can
                    // happen in several ways:
                    //  a. d's minSize increased to or past the old shared size
                    //  b. other long-pole definitions decreased, leaving
                    //      d as the new winner
                    // In the former case, the measure is valid - it used
                    // d's new larger minSize.  In the latter case, the
                    // measure is invalid - it used the old shared size,
                    // which is larger than d's (possibly changed) minSize
                    measureIsValid = (definitionBase.LayoutWasUpdated &&
                                    MathUtilities.GreaterThanOrClose(definitionBase._minSize, MinSize));
                }

                if (!measureIsValid)
                {
                    definitionBase.Parent!.InvalidateMeasure();
                }
                else if (!MathUtilities.AreClose(sharedMinSize, definitionBase.SizeCache))
                {
                    //  if measure is valid then also need to check arrange.
                    //  Note: definitionBase.SizeCache is volatile but at this point 
                    //  it contains up-to-date final size
                    definitionBase.Parent!.InvalidateArrange();
                }

                // now we can restore the invariant, and clear the layout flag
                definitionBase.UseSharedMinimum = useSharedMinimum;
                definitionBase.LayoutWasUpdated = false;
            }

            _minSize = sharedMinSize;

            _layoutUpdatedHost = null;

            _broadcastInvalidation = true;
        }

        //  the scope this state belongs to
        private readonly SharedSizeScope _sharedSizeScope;

        //  Id of the shared size group this object is servicing
        private readonly string _sharedSizeGroupId;

        //  Registry of participating definitions
        private readonly List<NBDefinitionBase> _registry;

        //  Instance event handler for layout updated event
        private readonly EventHandler _layoutUpdated;

        //  Control for which layout updated event handler is registered
        private NBVisual? _layoutUpdatedHost;

        //  "true" when broadcasting of invalidation is needed
        private bool _broadcastInvalidation;

        //  "true" when _userSize is up to date        
        private bool _userSizeValid;

        //  shared state                 
        private GridLength _userSize;

        //  shared state            
        private double _minSize;
    }
}