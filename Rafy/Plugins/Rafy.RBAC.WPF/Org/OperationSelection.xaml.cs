﻿/*******************************************************
 * 
 * 作者：胡庆访
 * 创建时间：20120327
 * 说明：此文件只包含一个类，具体内容见类型注释。
 * 运行环境：.NET 4.0
 * 版本号：1.0.0
 * 
 * 历史记录：
 * 创建文件 胡庆访 20120327
 * 
*******************************************************/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Rafy.WPF;
using Rafy.Domain;
using Rafy.RBAC;
using Rafy.MetaModel.View;
using Rafy;
using Rafy.WPF.Controls;
using Rafy.MetaModel;

namespace Rafy.RBAC.WPF
{
    /// <summary>
    /// 由于操作列表需要显示所有可用的操作，并按模块、界面块分组，并使用勾选视图显示黑名单中的操作。
    /// 比较复杂，所以使用一个自定义界面。
    /// </summary>
    public partial class OperationSelection : UserControl
    {
        /// <summary>
        /// 分别是左边的模块列表和右边的功能列表。
        /// </summary>
        private ListLogicalView _naviModulesView, _opertaionsView;

        /// <summary>
        /// 底层数据只是一个功能禁用列表
        /// </summary>
        private OrgPositionOperationDenyList _denyList;

        private bool _isBinding;

        public OperationSelection()
        {
            InitializeComponent();

            this.InitControls();
        }

        private void InitControls()
        {
            //左边的导航
            this._naviModulesView = AutoUI.ViewFactory.CreateListView(typeof(ModuleAC));
            this._naviModulesView.DataLoader.LoadDataAsync();
            this._naviModulesView.Control.Loaded += (o, e) => this._naviModulesView.ExpandAll();
            this._naviModulesView.CurrentChanged += _naviModulesView_CurrentObjectChanged;
            this._naviModulesView.IsReadOnly = ReadOnlyStatus.ReadOnly;
            navigation.Content = this._naviModulesView.Control;

            //右边的结果列表
            this._opertaionsView = AutoUI.ViewFactory.CreateListView(typeof(OperationAC));
            this._opertaionsView.CheckingMode = CheckingMode.CheckingRow;
            this._opertaionsView.CheckItemsChanged += _opertaionsView_CheckItemsChanged;
            result.Child = this._opertaionsView.Control;
        }

        #region 公有方法

        public void ExpandModules()
        {
            this._naviModulesView.ExpandAll();
        }

        public void CollapseModules()
        {
            this._naviModulesView.CollapseAll();
        }

        internal void BindData(OrgPositionOperationDenyList denyList)
        {
            this._denyList = denyList;

            this.BindOperations();
        }

        #endregion

        private bool IsDataBound
        {
            get { return this._denyList != null; }
        }

        /// <summary>
        /// 左边导航面板选择时，变更右边的功能列表
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _naviModulesView_CurrentObjectChanged(object sender, EventArgs e)
        {
            this.BindOperations();
        }

        /// <summary>
        /// 在功能列表被选择时，更改底层的数据列表。
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _opertaionsView_CheckItemsChanged(object sender, CheckItemsChangedEventArgs e)
        {
            this.SyncUnderlyModel();
        }

        /// <summary>
        /// 界面绑定
        /// </summary>
        private void BindOperations()
        {
            var curModule = this._naviModulesView.Current as ModuleAC;

            if (!this.IsDataBound || curModule == null || !curModule.Core.HasUI)
            {
                this._opertaionsView.Data = null;
                return;
            }

            var list = curModule.OperationACList;

            this._opertaionsView.Data = list;

            this.InitCheckBoxes(list, curModule);
        }

        /// <summary>
        /// 初始化选择框：如果已经在禁用列表中，则把它的勾去除
        /// </summary>
        /// <param name="list"></param>
        private void InitCheckBoxes(OperationACList list, ModuleAC curModule)
        {
            try
            {
                this._isBinding = true;

                this._opertaionsView.SelectAll();

                var moduleKey = curModule.Core.KeyLabel;

                foreach (OperationAC item in list)
                {
                    foreach (OrgPositionOperationDeny deny in this._denyList)
                    {
                        if (item.IsSame(deny))
                        {
                            this._opertaionsView.SelectedEntities.Remove(item);
                            break;
                        }
                    }
                }
            }
            finally
            {
                this._isBinding = false;
            }
        }

        /// <summary>
        /// 根据当前的功能列表选择项，更改底层的数据列表。
        /// </summary>
        private void SyncUnderlyModel()
        {
            var curModule = this._naviModulesView.Current as ModuleAC;
            if (!this.IsDataBound || this._isBinding || curModule == null) { return; }

            //清空当前模块在 denyList 中的数据
            var moduleKey = curModule.KeyLabel.TranslateReverse();
            var moduleOperations = this._denyList.Cast<OrgPositionOperationDeny>().Where(d => d.ModuleKey == moduleKey).ToArray();
            foreach (var item in moduleOperations) { this._denyList.Remove(item); }

            //根据当前的选择项把数据重新加入到 denyList 中。
            var toDeny = this._opertaionsView.Data.Except(this._opertaionsView.SelectedEntities.Cast<Entity>());
            var moduleScopeTranslated = OperationAC.ModuleScope.Translate();
            foreach (OperationAC item in toDeny)
            {
                var deny = new OrgPositionOperationDeny
                {
                    ModuleKey = moduleKey,
                    OperationKey = item.OperationKey
                };

                if (item.ScopeKeyLabel != moduleScopeTranslated)
                {
                    deny.BlockKey = item.ScopeKeyLabel.TranslateReverse();
                }

                this._denyList.Add(deny);
            }
        }
    }
}