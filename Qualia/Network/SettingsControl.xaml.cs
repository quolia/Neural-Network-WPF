﻿using Qualia.Model;
using Qualia.Tools;
using System;
using System.Collections.Generic;

namespace Qualia.Controls
{
    sealed public partial class SettingsControl : BaseUserControl
    {
        public SettingsControl()
        {
            InitializeComponent();

            this.SetConfigParams(new()
            {
                CtlSkipRoundsToDrawErrorMatrix
                    .Initialize(defaultValue: 10000),

                CtlSkipRoundsToDrawNetworks
                    .Initialize(defaultValue: 10000),

                CtlSkipRoundsToDrawStatistics
                    .Initialize(defaultValue: 10000),

                CtlPreventComputerFromSleep
                    .Initialize(defaultValue: true)
                    .SetUIParam(Notification.ParameterChanged.PreventComputerFromSleep),

                new FakeValue(Notification.ParameterChanged.Settings)
            });
        }

        public override void SetOnChangeEvent(Action<Notification.ParameterChanged> onChanged)
        {
            this.SetUIHandler(onChanged);
            this.GetConfigParams().ForEach(p => p.SetOnChangeEvent(Value_OnChanged));

            Value_OnChanged(Notification.ParameterChanged.PreventComputerFromSleep);
        }

        private void Value_OnChanged(Notification.ParameterChanged param)
        {
            if (param == Notification.ParameterChanged.Settings)
            {
                OnChanged(Notification.ParameterChanged.Settings);
                return;
            }

            if (param == Notification.ParameterChanged.PreventComputerFromSleep)
            {
                var yes = CtlPreventComputerFromSleep.Value;
                try
                {
                    SystemTools.SetPreventComputerFromSleep(yes);
                    OnChanged(yes
                              ? Notification.ParameterChanged.PreventComputerFromSleep
                              : Notification.ParameterChanged.DisablePreventComputerFromSleep);
                }
                catch
                {
                    OnChanged(yes
                              ? Notification.ParameterChanged.DisablePreventComputerFromSleep
                              : Notification.ParameterChanged.PreventComputerFromSleep);
                }
            }
        }

        public SettingsModel GetModel()
        {
            return new()
            {
                SkipRoundsToDrawErrorMatrix = (int)CtlSkipRoundsToDrawErrorMatrix.Value,
                SkipRoundsToDrawNetworks = (int)CtlSkipRoundsToDrawNetworks.Value,
                SkipRoundsToDrawStatistics = (int)CtlSkipRoundsToDrawStatistics.Value
            };
        }
    }
}
