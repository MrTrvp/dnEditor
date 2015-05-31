﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using dnEditor.Handlers;
using dnEditor.Misc;
using dnEditor.Properties;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using dnlib.DotNet.Writer;
using ExceptionHandler = dnlib.DotNet.Emit.ExceptionHandler;

namespace dnEditor.Forms
{
    public partial class MainForm : Form, ITreeView, ITreeMenu
    {
        public static ListView ListAnalysis;
        public static Label LblAnalysis;
        public static bool HandleExpand = true;
        private static SearchType _searchType;

        public static DataGridView DgBody;
        public static DataGridView DgVariables;
        public static RichTextBox RtbILSpy;
        public static CurrentAssembly CurrentAssembly;
        public static TabControl TabControl;
        public static int EditedInstructionIndex;
        public static int EditedVariableIndex;
        public static int EditedExceptionHandlerIndex;
        public static Instruction NewInstruction;
        public static Local NewVariable;
        public static ExceptionHandler NewExceptionHandler;
        public static TreeView TreeView;
        public static ToolStrip ToolStrip;
        public static ContextMenuStrip InstructionMenuStrip;
        public static ContextMenuStrip TreeMenuStrip;
        public static ContextMenuStrip VariableMenu;
        public static ContextMenuStrip ExceptionHandlerMenu;
        private EditExceptionHandlerMode _editExceptionHandlerMode;
        private EditInstructionMode _editInstructionMode;
        private EditVariableMode _editVariableMode;
        private readonly List<Instruction> _copiedInstructions = new List<Instruction>();
        private readonly List<Local> _copiedVariables = new List<Local>();
        private readonly TreeViewHandler _treeViewHandler;

        public MainForm()
        {
            InitializeComponent();
            _treeViewHandler = new TreeViewHandler(treeView1, treeMenu);

            TabControl = tabControl1;
            ListAnalysis = listAnalysis;
            ListAnalysis.Columns[0].Width = -1;
            LblAnalysis = lblAnalysis;

            DgBody = dgBody;
            DgVariables = dgVariables;
            RtbILSpy = rtbILSpy;

            TreeView = treeView1;
            ToolStrip = toolStrip1;

            InstructionMenuStrip = instructionMenu;
            VariableMenu = variableMenu;
            ExceptionHandlerMenu = exceptionHandlerMenu;

            TreeMenuStrip = treeMenu;
            txtMagicRegex.Text = Settings.Default.MagicRegex;

            InitializeBody();

            cbSearchType.SelectedIndex = 0;
        }

        private void InitializeBody()
        {
            DataGridViewHandler.InitializeBody();
        }

        private void EditInstructionForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            if (NewInstruction == null) return;

            if (!CurrentAssembly.Method.HasBody)
                CurrentAssembly.Method.Body = new CilBody();

            switch (_editInstructionMode)
            {
                case EditInstructionMode.Add:
                    CurrentAssembly.Method.Body.Instructions.Add(NewInstruction);
                    break;
                case EditInstructionMode.Edit:
                    CurrentAssembly.Method.Body.Instructions[EditedInstructionIndex].OpCode =
                        NewInstruction.OpCode;
                    CurrentAssembly.Method.Body.Instructions[EditedInstructionIndex].Operand =
                        NewInstruction.Operand;
                    break;
                case EditInstructionMode.InsertAfter:
                    CurrentAssembly.Method.Body.Instructions.Insert(EditedInstructionIndex + 1, NewInstruction);
                    break;
                case EditInstructionMode.InsertBefore:
                    CurrentAssembly.Method.Body.Instructions.Insert(EditedInstructionIndex, NewInstruction);
                    break;
            }

            CurrentAssembly.Method.Body.UpdateInstructionOffsets();
            DataGridViewHandler.ReadMethod(CurrentAssembly.Method);
        }

        private void EditVariableForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            if (NewVariable == null) return;

            if (!CurrentAssembly.Method.HasBody)
                CurrentAssembly.Method.Body = new CilBody();

            switch (_editVariableMode)
            {
                case EditVariableMode.Add:
                    CurrentAssembly.Method.Body.Variables.Add(NewVariable);
                    break;
                case EditVariableMode.Edit:
                    CurrentAssembly.Method.Body.Variables[EditedVariableIndex] = NewVariable;
                    break;
                case EditVariableMode.InsertAfter:
                    CurrentAssembly.Method.Body.Variables.Insert(EditedVariableIndex + 1, NewVariable);
                    break;
                case EditVariableMode.InsertBefore:
                    CurrentAssembly.Method.Body.Variables.Insert(EditedVariableIndex, NewVariable);
                    break;
            }

            DataGridViewHandler.ReadMethod(CurrentAssembly.Method);
        }

        private void EditExceptionHandlerForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            if (NewExceptionHandler == null) return;

            if (!CurrentAssembly.Method.HasBody)
                CurrentAssembly.Method.Body = new CilBody();

            switch (_editExceptionHandlerMode)
            {
                case EditExceptionHandlerMode.Add:
                    CurrentAssembly.Method.Body.ExceptionHandlers.Add(NewExceptionHandler);
                    break;
                case EditExceptionHandlerMode.Edit:
                    CurrentAssembly.Method.Body.ExceptionHandlers[EditedExceptionHandlerIndex] =
                        NewExceptionHandler;
                    break;
            }

            CurrentAssembly.Method.Body.UpdateInstructionOffsets();
            DataGridViewHandler.ReadMethod(CurrentAssembly.Method);
        }

        private void NewInstructionEditor(EditInstructionMode mode)
        {
            NewInstruction = null;
            _editInstructionMode = mode;

            if (dgBody.SelectedRows.Count > 0)
                EditedInstructionIndex = dgBody.SelectedRows.TopmostRow().Index;

            if (mode == EditInstructionMode.Edit)
            {
                var form =
                    new EditInstructionForm(
                        CurrentAssembly.Method.Body.Instructions[dgBody.SelectedRows.TopmostRow().Index]);
                form.FormClosed += EditInstructionForm_FormClosed;
                form.ShowDialog();
            }
            else
            {
                var form = new EditInstructionForm();
                form.FormClosed += EditInstructionForm_FormClosed;
                form.ShowDialog();
            }
        }

        private void NewVariableEditor(EditVariableMode mode)
        {
            NewVariable = null;
            _editVariableMode = mode;

            if (dgVariables.SelectedRows.Count > 0)
                EditedVariableIndex = dgVariables.SelectedRows.TopmostRow().Index;

            if (mode == EditVariableMode.Edit)
            {
                var form =
                    new EditVariableForm(
                        CurrentAssembly.Method.Body.Variables[dgVariables.SelectedRows.TopmostRow().Index]);
                form.FormClosed += EditVariableForm_FormClosed;
                form.ShowDialog();
            }
            else
            {
                var form = new EditVariableForm();
                form.FormClosed += EditVariableForm_FormClosed;
                form.ShowDialog();
            }
        }

        private void NewExceptionHandlerEditor(EditExceptionHandlerMode mode)
        {
            NewExceptionHandler = null;
            _editExceptionHandlerMode = mode;
            DataGridViewSelectedRowCollection selectedRows = dgBody.SelectedRows;

            if (selectedRows.Count > 0)
                EditedExceptionHandlerIndex = dgBody.SelectedRows.TopmostRow().Index -
                                              CurrentAssembly.Method.Body.Instructions.Count;

            if (mode == EditExceptionHandlerMode.Edit)
            {
                var form =
                    new EditExceptionHandlerForm(
                        CurrentAssembly.Method.Body.ExceptionHandlers[
                            (dgBody.SelectedRows.TopmostRow().Index -
                             CurrentAssembly.Method.Body.Instructions.Count)]);
                form.FormClosed += EditExceptionHandlerForm_FormClosed;
                form.ShowDialog();
            }
            else
            {
                var form = new EditExceptionHandlerForm();
                form.FormClosed += EditExceptionHandlerForm_FormClosed;
                form.ShowDialog();
            }
        }

        private void txtMagicRegex_TextChanged(object sender, EventArgs e)
        {
            Settings.Default.MagicRegex = txtMagicRegex.Text;
            Settings.Default.Save();
        }

        private void tabControl1_Selected(object sender, TabControlEventArgs e)
        {
            ILSpyHandler.CheckDecompile();
        }

        private void rtbILSpy_MouseDown(object sender, MouseEventArgs e)
        {
            if (!rtbILSpy.AutoWordSelection)
            {
                rtbILSpy.AutoWordSelection = false;
            }
        }

        private void lblMagicRegex_Click(object sender, EventArgs e)
        {
            txtMagicRegex.Text = Settings.Default.MagicRegex = @"[^\x20-\x7F]";
            Settings.Default.Save();
        }

        #region ToolStrip

        private void btnSave_Click(object sender, EventArgs e)
        {
            if (CurrentAssembly == null) return;

            new WriteAssemblyForm(CurrentAssembly.ManifestModule).ShowDialog();
        }

        private void btnOpen_Click(object sender, EventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Title = "Choose an assembly to open...",
                Filter = ".NET Assemblies (*.exe;*.dll)|*.exe;*.dll"
            };

            if (dialog.ShowDialog() != DialogResult.OK || !File.Exists(dialog.FileName))
                return;

            Functions.OpenFile(_treeViewHandler, dialog.FileName, ref CurrentAssembly);
        }

        private void btnAbout_Click(object sender, EventArgs e)
        {
            MessageBox.Show(@"dnEditor is a .NET assembly editor based on dnlib.

Coded, maintained and organized by ViRb3 (virb3e@gmail.com)

GitHub project page: https://github.com/ViRb3/dnEditor

Copyright (C) 2014-2015 ViRb3
Licenses can be found in the root directory of the project.", "About dnEditor");
        }

        public void HandleToolStripItemsState()
        {
            if (TreeView.Nodes.Count == 0)
            {
                btnNavigateBack.Enabled = false;
                btnNavigateForward.Enabled = false;
                btnSave.Enabled = false;
                cbSearchType.Enabled = false;
                txtSearch.Enabled = false;
                btnSearch.Enabled = false;
            }
            else
            {
                btnNavigateBack.Enabled = _treeViewHandler.NavigationHistory.HasPast;
                btnNavigateForward.Enabled = _treeViewHandler.NavigationHistory.HasFuture;
                btnSave.Enabled = true;
                cbSearchType.Enabled = true;
                txtSearch.Enabled = true;
                btnSearch.Enabled = true;
            }
        }

        private void btnNavigateBack_Click(object sender, EventArgs e)
        {
            _treeViewHandler.NavigationHistory.GoBack();
        }

        private void btnNavigateForward_Click(object sender, EventArgs e)
        {
            _treeViewHandler.NavigationHistory.GoForward();
        }

        #endregion ToolStrip

        #region InstructionMenuStrip

        private void editToolStripMenuItem_Click(object sender, EventArgs e)
        {
            NewInstructionEditor(EditInstructionMode.Edit);
        }

        private void insertBeforeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            NewInstructionEditor(EditInstructionMode.InsertBefore);
        }

        private void insertAfterToolStripMenuItem_Click(object sender, EventArgs e)
        {
            NewInstructionEditor(EditInstructionMode.InsertAfter);
        }

        private void nopToolStripMenuItem_Click(object sender, EventArgs e)
        {
            DataGridViewSelectedRowCollection collection = dgBody.SelectedRows;

            foreach (DataGridViewRow row in collection)
            {
                int instructionIndex = CurrentAssembly.Method.Body.Instructions.IndexOf(row.Tag as Instruction);
                CurrentAssembly.Method.Body.Instructions[instructionIndex].OpCode = OpCodes.Nop;
                CurrentAssembly.Method.Body.Instructions[instructionIndex].Operand = null;
            }

            CurrentAssembly.Method.Body.UpdateInstructionOffsets();

            DataGridViewHandler.ReadMethod(CurrentAssembly.Method);
        }

        private void cutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _copiedInstructions.Clear();

            foreach (DataGridViewRow selectedRow in dgBody.SelectedRows)
            {
                _copiedInstructions.Add(selectedRow.Tag as Instruction);
                CurrentAssembly.Method.Body.Instructions.RemoveAt(selectedRow.Index);
            }

            CurrentAssembly.Method.Body.Instructions.UpdateInstructionOffsets();
            DataGridViewHandler.ReadMethod(CurrentAssembly.Method);
        }

        private void copyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _copiedInstructions.Clear();

            foreach (DataGridViewRow selectedRow in dgBody.SelectedRows)
            {
                _copiedInstructions.Add(selectedRow.Tag as Instruction);
            }
        }

        private void pasteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            int instructionIndex = dgBody.SelectedRows.TopmostRow().Index + 1;

            foreach (Instruction instruction in _copiedInstructions.OrderByDescending(i => i.Offset))
            {
                var newInstruction = new Instruction(instruction.OpCode);

                if (instruction.Operand != null)
                    switch (instruction.OpCode.OperandType)
                    {
                        case OperandType.InlineField:
                            var field = instruction.Operand as IField;
                            if (field.DeclaringType.DefinitionAssembly != CurrentAssembly.ManifestModule.Assembly ||
                                field.Module != CurrentAssembly.ManifestModule)
                            {
                                var fieldRef = new MemberRefUser(field.Module, field.Name, field.FieldSig,
                                    field.DeclaringType);

                                newInstruction.Operand = CurrentAssembly.ManifestModule.Import(fieldRef);
                            }
                            else
                            {
                                newInstruction.Operand = CurrentAssembly.ManifestModule.ResolveField(field.Rid);
                            }
                            break;

                        case OperandType.InlineMethod:
                            var method = instruction.Operand as IMethod;
                            if (method.DeclaringType.DefinitionAssembly != CurrentAssembly.ManifestModule.Assembly ||
                                method.Module != CurrentAssembly.ManifestModule)
                            {
                                var methodRef = new MemberRefUser(method.Module, method.Name, method.MethodSig,
                                    method.DeclaringType);

                                newInstruction.Operand = CurrentAssembly.ManifestModule.Import(methodRef);
                            }
                            else
                            {
                                newInstruction.Operand = CurrentAssembly.ManifestModule.ResolveMethod(method.Rid);
                            }
                            break;

                        case OperandType.InlineType:
                            var type = instruction.Operand as ITypeDefOrRef;

                            if (type.DefinitionAssembly != CurrentAssembly.ManifestModule.Assembly ||
                                type.Module != CurrentAssembly.ManifestModule)
                            {
                                var typeRef = new TypeRefUser(type.Module, type.Namespace, type.Name,
                                    CurrentAssembly.ManifestModule.CorLibTypes.AssemblyRef);

                                newInstruction.Operand = CurrentAssembly.ManifestModule.Import(typeRef);
                            }
                            else
                            {
                                newInstruction.Operand = CurrentAssembly.ManifestModule.ResolveTypeDefOrRef(type.Rid);
                            }
                            break;
                        default:
                            newInstruction.Operand = instruction.Operand;
                            break;
                    }

                CurrentAssembly.Method.Body.Instructions.Insert(instructionIndex, newInstruction);
            }
            FixBranches();

            CurrentAssembly.Method.Body.Instructions.UpdateInstructionOffsets();
            DataGridViewHandler.ReadMethod(CurrentAssembly.Method);
        }

        private void FixBranches()
        {
            foreach (Instruction instruction in CurrentAssembly.Method.Body.Instructions)
            {
                if (instruction.OpCode.OperandType == OperandType.InlineBrTarget ||
                    instruction.OpCode.OperandType == OperandType.ShortInlineBrTarget)
                {
                    //TODO: Fix for cases where >1 instructions have the same OpCode and Operand
                    if (CurrentAssembly.Method.Body.Instructions.Count(i => i.OpCode == (instruction.Operand as Instruction).OpCode &&
                        i.Operand == (instruction.Operand as Instruction).Operand) == 1)
                    {
                        instruction.Operand = CurrentAssembly.Method.Body.Instructions.First(i => i.OpCode == (instruction.Operand as Instruction).OpCode &&
                        i.Operand == (instruction.Operand as Instruction).Operand);
                    }
                }
            }
        }

        private void deleteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            DataGridViewSelectedRowCollection collection = dgBody.SelectedRows;

            foreach (DataGridViewRow row in collection)
            {
                CurrentAssembly.Method.Body.Instructions.Remove(row.Tag as Instruction);
            }

            CurrentAssembly.Method.Body.UpdateInstructionOffsets();

            DataGridViewHandler.ReadMethod(CurrentAssembly.Method);
        }

        private void saveInstructionsToFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!CurrentAssembly.Method.HasBody || !CurrentAssembly.Method.Body.HasInstructions)
                return;

            var code = new StringBuilder();
            code.AppendLine("//===================================");
            code.AppendLine("//dnlib");
            code.AppendLine("//===================================");
            code.AppendLine("");
            var i = 0;

            if (CurrentAssembly.Method.Body.Variables.Count > 0)
            {
                code.AppendLine(String.Format("List<Local> locals = new List<Local>();"));

                foreach (Local local in CurrentAssembly.Method.Body.Variables)
                {
                    code.AppendLine(String.Format("locals.Add(new Local(\"{0}\"));",
                        local.Type.FullName.Replace("\"", "``")));
                    i++;
                }

                code.AppendLine("");
                code.AppendLine("");
                i = 0;
            }

            code.AppendLine(String.Format("List<Instruction> instructions = new List<Instruction>();"));

            foreach (Instruction instruction in CurrentAssembly.Method.Body.Instructions)
            {
                OpCode opCode = Functions.OpCodes.First(o => o.Name == instruction.OpCode.Name);

                if (instruction.Operand != null)
                    code.AppendLine(String.Format("instructions.Add(OpCodes.{0}.ToInstruction(\"{1}\"));", opCode.Code,
                        instruction.Operand.ToString().Replace("\"", "``")));
                else
                    code.AppendLine(String.Format("instructions.Add(OpCodes.{1}.ToInstruction());", i, opCode.Code));

                i++;
            }

            code.AppendLine("");
            code.AppendLine("");
            code.AppendLine("");
            code.AppendLine("//===================================");
            code.AppendLine("//System.Reflection");
            code.AppendLine("//===================================");
            code.AppendLine("");

            if (CurrentAssembly.Method.Body.Variables.Count > 0)
            {
                foreach (Local local in CurrentAssembly.Method.Body.Variables)
                {
                    code.AppendLine(String.Format("il.DeclareLocal(\"{0}\"));", local.Type.FullName.Replace("\"", "``")));
                    i++;
                }

                code.AppendLine("");
                code.AppendLine("");
                i = 0;
            }

            foreach (Instruction instruction in CurrentAssembly.Method.Body.Instructions)
            {
                OpCode opCode = Functions.OpCodes.First(o => o.Name == instruction.OpCode.Name);

                if (instruction.Operand != null)
                    code.AppendLine(String.Format("il.Emit(OpCodes.{0}, \"{1}\");", opCode.Code,
                        instruction.Operand.ToString().Replace("\"", "``")));
                else
                    code.AppendLine(String.Format("il.Emit(OpCodes.{0});", opCode.Code));

                i++;
            }

            File.WriteAllText("instructions.txt", code.ToString());
            MessageBox.Show("Instructions saved to \"instructions.txt\"!", "Success", MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        #endregion InstructionMenuStrip

        #region VariableMenuStrip

        private void editVariableToolStripMenuItem_Click(object sender, EventArgs e)
        {
            NewVariableEditor(EditVariableMode.Edit);
        }

        private void insertVariableBeforeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            NewVariableEditor(EditVariableMode.InsertBefore);
        }

        private void insertVariableAfterToolStripMenuItem_Click(object sender, EventArgs e)
        {
            NewVariableEditor(EditVariableMode.InsertAfter);
        }

        private void cutVariableToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _copiedVariables.Clear();

            foreach (DataGridViewRow selectedRow in dgVariables.SelectedRows)
            {
                _copiedVariables.Add(selectedRow.Tag as Local);
                CurrentAssembly.Method.Body.Variables.RemoveAt(selectedRow.Index);
            }

            DataGridViewHandler.ReadMethod(CurrentAssembly.Method);
        }

        private void copyVariableToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _copiedVariables.Clear();

            foreach (DataGridViewRow selectedRow in dgVariables.SelectedRows)
            {
                _copiedVariables.Add(selectedRow.Tag as Local);
            }
        }

        private void pasteVariableToolStripMenuItem_Click(object sender, EventArgs e)
        {
            int variableIndex = dgVariables.SelectedRows.TopmostRow().Index + 1;

            foreach (Local variable in _copiedVariables)
            {
                var newVariable = new Local(variable.Type);

                if (variable.Name != null)
                    newVariable.Name = variable.Name;

                CurrentAssembly.Method.Body.Variables.Insert(variableIndex, newVariable);
            }

            DataGridViewHandler.ReadMethod(CurrentAssembly.Method);
        }

        private void deleteVariableToolStripMenuItem_Click(object sender, EventArgs e)
        {
            DataGridViewSelectedRowCollection collection = dgVariables.SelectedRows;

            foreach (DataGridViewRow row in collection)
            {
                CurrentAssembly.Method.Body.Variables.Remove(row.Tag as Local);
            }

            DataGridViewHandler.ReadMethod(CurrentAssembly.Method);
        }

        #endregion VariableMenuStrip

        #region ExceptionHandlerMenuStrip

        private void editToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            NewExceptionHandlerEditor(EditExceptionHandlerMode.Edit);
        }

        private void removeToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            DataGridViewSelectedRowCollection collection = dgBody.SelectedRows;

            foreach (DataGridViewRow row in collection)
            {
                CurrentAssembly.Method.Body.ExceptionHandlers.Remove(row.Tag as ExceptionHandler);
            }

            DataGridViewHandler.ReadMethod(CurrentAssembly.Method);
        }

        #endregion ExceptionHandlerMenuStrip

        #region TreeMenuStrip

        public void treeMenu_Opened(object sender, EventArgs e)
        {
            _treeViewHandler.treeMenu_Opened(sender, e);
        }

        public void goToEntryPointToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _treeViewHandler.goToEntryPointToolStripMenuItem_Click(sender, e);
        }

        public void goToModuleCCtorToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _treeViewHandler.goToModuleCCtorToolStripMenuItem_Click(sender, e);
        }

        public void expandToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _treeViewHandler.expandToolStripMenuItem_Click(sender, e);
        }

        public void collapseToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _treeViewHandler.collapseToolStripMenuItem_Click(sender, e);
        }

        public void collapseAllToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _treeViewHandler.collapseAllToolStripMenuItem_Click(sender, e);
        }

        public void closeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _treeViewHandler.closeToolStripMenuItem_Click(sender, e, ref CurrentAssembly);
            HandleToolStripItemsState();
        }

        #endregion TreeMenuStrip

        #region EmptyInstructionsMenu

        private void createNewInstructionToolStripMenuItem_Click(object sender, EventArgs e)
        {
            NewInstructionEditor(EditInstructionMode.Add);
        }

        private void emptyBodyMenu_Opened(object sender, EventArgs e)
        {
            if (CurrentAssembly == null || CurrentAssembly.Method == null || CurrentAssembly.Method == null)
            {
                emptyBodyMenu.Items[0].Enabled = false;
                emptyBodyMenu.Items[1].Enabled = false;
                return;
            }

            emptyBodyMenu.Items[0].Enabled = true;
            emptyBodyMenu.Items[1].Enabled = true;
        }

        private void createNewExceptionHandlerToolStripMenuItem_Click(object sender, EventArgs e)
        {
            NewExceptionHandlerEditor(EditExceptionHandlerMode.Add);
        }

        #endregion EmptyInstructionsMenu

        #region TreeView Events

        private void treeView_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Delete && _treeViewHandler.SelectedNode != null &&
                _treeViewHandler.SelectedNode.Tag is ModuleDefMD)
            {
                closeToolStripMenuItem_Click(sender, e);
            }
            else
            {
                TreeNode node = null;
                if (e.KeyCode == Keys.Up)
                    node = treeView1.SelectedNode.PrevNode;
                else if (e.KeyCode == Keys.Down)
                    node = treeView1.SelectedNode.NextNode;

                if (node != null)
                    _treeViewHandler.treeView_NodeMouseClick(sender,
                        new TreeNodeMouseClickEventArgs(node, MouseButtons.Left, 0, 0, 0));
            }
        }

        public void treeView_AfterExpand(object sender, TreeViewEventArgs e)
        {
            if (HandleExpand)
            _treeViewHandler.treeView1_AfterExpand(sender, e);
        }

        public void treeView_DragDrop(object sender, DragEventArgs e)
        {
            string result = _treeViewHandler.DragDrop(sender, e);
            if (!String.IsNullOrEmpty(result))
            {
                Functions.OpenFile(_treeViewHandler, result, ref CurrentAssembly);
            }
        }

        public void treeView_DragEnter(object sender, DragEventArgs e)
        {
            _treeViewHandler.DragEnter(sender, e);
        }

        public void treeView_NodeMouseClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            _treeViewHandler.treeView_NodeMouseClick(sender, e);
        }

        public void treeView_NodeMouseDoubleClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            _treeViewHandler.treeView_NodeMouseDoubleClick(sender, e, ref CurrentAssembly);
        }

        public void treeView1_NodeMouseHover(object sender, TreeNodeMouseHoverEventArgs e)
        {
            _treeViewHandler.treeView1_NodeMouseHover(sender, e);
        }

        #endregion TreeView Events

        #region DataGridView Events

        private void dgBody_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex > CurrentAssembly.Method.Body.Instructions.Count - 1)
            {
                NewExceptionHandlerEditor(EditExceptionHandlerMode.Edit);
                return;
            }

            if (e.ColumnIndex == 1 || e.ColumnIndex == 2)
            {
                NewInstructionEditor(EditInstructionMode.Edit);
                return;
            }

            if (e.ColumnIndex == 3)
            {
                var instruction = dgBody.Rows[e.RowIndex].Tag as Instruction;

                if (!(instruction.Operand is Instruction))
                    return;

                foreach (DataGridViewRow row in dgBody.Rows)
                {
                    if (row.Tag == instruction.Operand as Instruction)
                    {
                        dgBody.FirstDisplayedScrollingRowIndex = row.Index;
                        dgBody.ClearSelection();
                        row.Selected = true;

                        return;
                    }
                }
            }
        }

        private void dgVariables_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            NewVariableEditor(EditVariableMode.Edit);
        }

        private void dgBody_CellMouseDown(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right) return;

            if (dgBody.SelectedRows.Cast<DataGridViewRow>().Any(selectedRow => selectedRow.Index == e.RowIndex))
                return;

            dgBody.ClearSelection();

            dgBody.Rows[e.RowIndex].Selected = true;
        }

        private void dgVariables_CellMouseDown(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right) return;

            if (dgVariables.SelectedRows.Cast<DataGridViewRow>().Any(selectedRow => selectedRow.Index == e.RowIndex))
                return;

            dgVariables.ClearSelection();

            dgVariables.Rows[e.RowIndex].Selected = true;
        }

        private void dgBody_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                emptyBodyMenu.Show();
            }
        }

        private void dgVariables_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                emptyVariableMenu.Show();
            }
        }

        #endregion DataGridView Events 

        #region EmptyVariablesMenu

        private void createNewVariableToolStripMenuItem_Click(object sender, EventArgs e)
        {
            NewVariableEditor(EditVariableMode.Add);
        }

        private void createNewVariableToolStripMenuItem_Opened(object sender, EventArgs e)
        {
            if (CurrentAssembly == null || CurrentAssembly.Method == null)
            {
                emptyVariableMenu.Items[0].Enabled = false;
                return;
            }

            emptyVariableMenu.Items[0].Enabled = true;
        }

        #endregion EmptyInstructionsMenu        

        #region Search

        private void listAnalysis_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            ListViewItem item = listAnalysis.GetItemAt(e.X, e.Y);

            if (item == null)
                return;

            _treeViewHandler.BrowseAndExpandMember(item.Tag);
        }

        public void SearchFinished(object result)
        {
            if (result is int) // row index
            {
                int i = int.Parse(result.ToString());

                DgBody.ClearSelection();
                DgBody.FirstDisplayedScrollingRowIndex = i;
                DgBody.Rows[i].Selected = true;
            }
            else if (result is List<object>) // members
            {
                var resultList = result as List<object>;

                if (resultList.Count == 0)
                    return;

                AnalysisHandler.UpdateStatus("Search results");

                ListAnalysis.BeginUpdate();

                foreach (object item in resultList)
                {
                    var listViewItem = new ListViewItem();
                    listViewItem.Text = item.ToString();
                    listViewItem.Tag = item;

                    ListAnalysis.Items.Add(listViewItem);
                }

                ListAnalysis.EndUpdate();
                ListAnalysis.Columns[0].AutoResize(ColumnHeaderAutoResizeStyle.ColumnContent);
            }
            else if (result is Dictionary<object, string>) // string result member
            {
                var resultList = result as Dictionary<object, string>;

                if (resultList.Count == 0)
                    return;

                AnalysisHandler.UpdateStatus("Search results");

                ListAnalysis.BeginUpdate();

                foreach (KeyValuePair<object, string> pair in resultList)
                {
                    var listViewItem = new ListViewItem();
                    listViewItem.Text = string.Format("{0} --> {1}", pair.Key, pair.Value);
                    listViewItem.Tag = pair.Key;

                    ListAnalysis.Items.Add(listViewItem);
                }

                ListAnalysis.EndUpdate();
                ListAnalysis.Columns[0].AutoResize(ColumnHeaderAutoResizeStyle.ColumnContent);
            }
        }

        private void btnSearch_Click(object sender, EventArgs e)
        {
            if (cbSearchType.Text.Trim() == "" || txtSearch.Text.Trim() == "") return;
            TreeNode searchNode;

            switch (cbSearchType.Text)
            {
                case "MDToken":
                    _searchType = SearchType.MDToken;
                    searchNode = _treeViewHandler.CurrentModule;
                    break;

                case "String (global)":
                    _searchType = SearchType.StringGlobal;
                    searchNode = _treeViewHandler.CurrentModule;
                    break;

                case "String":
                    _searchType = SearchType.String;
                    searchNode = _treeViewHandler.CurrentMethod;
                    break;

                case "OpCode":
                    _searchType = SearchType.OpCode;
                    searchNode = _treeViewHandler.CurrentMethod;
                    break;

                case "Operand":
                    _searchType = SearchType.Operand;
                    searchNode = _treeViewHandler.CurrentMethod;
                    break;

                default:
                    _searchType = SearchType.Any;
                    searchNode = _treeViewHandler.CurrentModule;
                    break;
            }

            if (searchNode == null) return;

            var searchHandler = new SearchHandler(searchNode, txtSearch.Text, _searchType, _treeViewHandler);
            searchHandler.SearchFinished += SearchFinished;
            searchHandler.Search();

            DgBody.Focus();
        }

        #endregion Search
    }
}
