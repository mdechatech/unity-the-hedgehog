﻿using System;
using SonicRealms.Core.Moves;
using SonicRealms.Core.Utils;
using UnityEngine;

namespace SonicRealms.Core.Actors
{
    /// <summary>
    /// Controls sprite rotation based on the controller's state.
    /// </summary>
    public class HedgehogRotation : MonoBehaviour
    {
        public HedgehogController Controller;
        public Transform RendererTransform;

        [NonSerialized] public float TrueRotation;
        [NonSerialized] public float IntervalRotation;
        public float Rotation
        {
            get { return RendererTransform.eulerAngles.z; }
            set
            {
                RendererTransform.eulerAngles = new Vector3(
                    RendererTransform.eulerAngles.x,
                    RendererTransform.eulerAngles.y,
                    value);
            }
        }

        /// <summary>
        /// Whether to rotate to the direction of gravity when not rotating to surface angle.
        /// </summary>
        [Tooltip("Whether to rotate to the direction of gravity when not rotating to surface angle.")]
        public bool RotateToGravity;

        /// <summary>
        /// Whether to rotate to surface angle during a roll.
        /// </summary>
        [Tooltip("Whether to rotate to surface angle during a roll.")]
        public bool RotateDuringRoll;

        /// <summary>
        /// Whether to rotate to surface angle when standing.
        /// </summary>
        [Tooltip("Whether to rotate to surface angle when standing.")]
        public bool RotateDuringStand;

        /// <summary>
        /// How quickly the controller rotates back to normal after leaving the ground, in degrees per second.
        /// </summary>
        [Tooltip("How quickly the controller rotates back to normal after leaving the ground, in degrees per second.")]
        public float AirRecoveryRate;
        protected float AirRotation;

        /// <summary>
        /// Rotates to surface angle if it is this much different than the direction of gravity, in degrees.
        /// Makes it so the controller doesn't rotate when the ground is flat.
        /// </summary>
        [Tooltip("Rotates to surface angle if it is this much different than the direction of gravity, in degrees. " +
            "Makes it so the controller doesn't rotate when the ground is flat.")]
        public float MinimumAngle;

        /// <summary>
        /// Intervals between the different orientations of the player, in degrees. For example:
        /// if this is 45, the player will rotate its sprite every 45 degrees.
        /// </summary>
        [Tooltip("Intervals between the different orientations of the player, in degrees. For example: " +
                 "if this is 45, the player will rotate its sprite every 45 degrees.")]
        public float IntervalAngle;

        [Range(0f, 1f)]
        [Tooltip("Prevents twitching between different orientations. The player must overcome this much of the " +
                 "rotation interval to change its sprite rotation.")]
        public float IntervalThreshold;

        protected float IntervalMin;
        protected float Interval;
        protected float IntervalMax;

        protected Roll Roll;
        protected GroundControl GroundControl;

        public void Reset()
        {
            RotateToGravity = true;
            RotateDuringRoll = false;
            RotateDuringStand = false;
            MinimumAngle = 22.5f;
            AirRecoveryRate = 360.0f;

            IntervalAngle = 45f;
            IntervalThreshold = 0.1f;

            Controller = GetComponentInParent<HedgehogController>();
            if (Controller == null) return;
            RendererTransform = Controller.RendererObject.transform;
        }

        public void Awake()
        {
            TrueRotation = 0.0f;
        }

        public void Start()
        {
            RendererTransform = RendererTransform ?? Controller.RendererObject.transform;
            Roll = Controller.GetMove<Roll>();
            GroundControl = Controller.GetMove<GroundControl>();
            UpdateInterval();
        }

        public void Update()
        {
            if (Controller.Grounded)
            {
                // A whole bunch of conditions to see if we should or shouldn't rotate
                var rotateToSensors = true;
                rotateToSensors &= RotateDuringStand ||
                                   (GroundControl == null
                                       ? !DMath.Equalsf(Controller.GroundVelocity)
                                       : !GroundControl.Standing);
                rotateToSensors &= Roll == null || RotateDuringRoll || !Roll.Active;
                rotateToSensors &=
                    Mathf.Abs(DMath.ShortestArc_d(Controller.SensorsRotation, Controller.GravityRight)) >
                    MinimumAngle;

                TrueRotation = rotateToSensors ? Controller.SensorsRotation : Controller.GravityRight;
            }
            else
            {
                if ((RotateDuringStand || !DMath.Equalsf(Controller.GroundVelocity)) &&
                    (Roll == null || RotateDuringRoll || !Roll.Active))
                {
                    var difference = DMath.ShortestArc_d(Rotation, Controller.GravityRight);
                    difference = difference > 0.0f
                        ? Mathf.Min(AirRecoveryRate * Time.deltaTime, difference)
                        : Mathf.Max(-AirRecoveryRate * Time.deltaTime, difference);

                    TrueRotation += difference;
                }
                else
                {
                    TrueRotation = Controller.GravityRight;
                }
            }

            FixRotation();
        }

        /// <summary>
        /// Rounds rotation to the nearest precision.
        /// </summary>
        public void FixRotation()
        {
            if (!DMath.AngleInRange_d(TrueRotation, IntervalMin, IntervalMax))
            {
                UpdateInterval();
            }
            
            Rotation = Interval;
        }

        public void UpdateInterval()
        {
            Interval = DMath.Round(TrueRotation, IntervalAngle, Controller.GravityRight);

            if (DMath.AngleInRange_d(Interval, -MinimumAngle, MinimumAngle))
            {
                IntervalMin = 360f - MinimumAngle;
                IntervalMax = MinimumAngle;
            }
            else if (DMath.AngleInRange_d(Interval, -MinimumAngle - IntervalAngle, -MinimumAngle))
            {
                // When one interval below 0
                IntervalMin = DMath.PositiveAngle_d(Interval - IntervalAngle*(0.5f + IntervalThreshold));
                IntervalMax = 360f - MinimumAngle + IntervalAngle*IntervalThreshold;
            }
            else if (DMath.AngleInRange_d(Interval, MinimumAngle, MinimumAngle + IntervalAngle))
            {
                // When one interval above 0
                IntervalMin = MinimumAngle - IntervalAngle*IntervalThreshold;
                IntervalMax = DMath.PositiveAngle_d(Interval + IntervalAngle * (0.5f + IntervalThreshold));
            }
            else
            {
                IntervalMin = DMath.PositiveAngle_d(Interval - IntervalAngle*(0.5f + IntervalThreshold));
                IntervalMax = DMath.PositiveAngle_d(Interval + IntervalAngle*(0.5f + IntervalThreshold));
            }
        }
    }
}
