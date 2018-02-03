﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Domain.Entities;
using Domain.Entities.Link;
using Attribute = Domain.Entities.Attribute.Attribute;

// Check connection string!

namespace Forms
{
    public partial class MainForm : Form
    {
        private readonly App.App _app;
        private readonly List<string> _nodes = new List<string>();

        public MainForm(App.App app)
        {
            _app = app;
            InitializeComponent();
        }

        private void CreateDatabase_Click(object sender, EventArgs e)
        {
            _nodes.Clear();
            DatabaseCreationForm databaseForm = new DatabaseCreationForm();
            if (DatabasesTree.Nodes.Count != 0)
                foreach (TreeNode treeviewNodes in DatabasesTree.Nodes)
                    _nodes.Add(treeviewNodes.Text);

            databaseForm.SetNodes(_nodes);
            databaseForm.Show();
            databaseForm.Text = @"База данных";
            databaseForm.dbNameLabel.Text = @"Введите название базы данных";
            databaseForm.FormClosing += (obj, args) =>
            {
                TreeNode newNode = new TreeNode();

                if (databaseForm.InputText == string.Empty) return;

                try
                {
                    _app.CreateDatabase(databaseForm.InputText);
                }
                catch (ArgumentException exception)
                {
                    MessageBox.Show(exception.Message);
                }

                newNode.Text = databaseForm.InputText;
                DatabasesTree.Nodes.Add(newNode);


                DatabasesTree.ExpandAll();
                DatabasesTree.Update();
            };
        }

        private void CreateTable_Click(object sender, EventArgs e)
        {
            if (DatabasesTree.SelectedNode != null && DatabasesTree.SelectedNode.Level == 0)
            {
                _nodes.Clear();
                DatabaseCreationForm tableForm = new DatabaseCreationForm();
                if (DatabasesTree.Nodes.Count != 0)
                {
                    TreeNode treeviewNodes = DatabasesTree.SelectedNode != null && DatabasesTree.SelectedNode.Level == 0
                        ? DatabasesTree.SelectedNode
                        : DatabasesTree.TopNode;
                    foreach (TreeNode node in treeviewNodes.Nodes) _nodes.Add(node.Text);
                }

                tableForm.SetNodes(_nodes);
                tableForm.Show();
                tableForm.Text = @"Таблица";
                tableForm.dbNameLabel.Text = @"Введите название таблицы";
                tableForm.FormClosing += (obj, args) =>
                {
                    TreeNode newNode = new TreeNode();

                    if (tableForm.InputText == string.Empty) return;

                    Database database = _app.GetDatabaseByName(DatabasesTree.SelectedNode.Text);
                    try
                    {
                        _app.AddTable(database, tableForm.InputText);
                        newNode.Text = tableForm.InputText;
                        DatabasesTree.SelectedNode.Nodes.Add(newNode);
                    }
                    catch (ArgumentException exception)
                    {
                        MessageBox.Show(exception.Message);
                    }

                    DatabasesTree.ExpandAll();
                    DatabasesTree.Update();
                };
            }
            else
            {
                MessageBox.Show(@"Выберите базу данных!");
            }
        }

        private void CreateAttribute_Click(object sender, EventArgs e)
        {
            if (DatabasesTree.SelectedNode != null && DatabasesTree.SelectedNode.Level == 1)
            {
                Dictionary<string, string> attribute;
                AttributeCreationForm attributeForm = new AttributeCreationForm();
                List<TreeNode> nodesList = new List<TreeNode>();
                nodesList.AddRange(
                    DatabasesTree.Nodes
                        .OfType<TreeNode>()
                        .Where(x => x.Text == @"PrimaryKey")
                        .Select(x => x.Parent)
                        .ToArray()
                );
                foreach (TreeNode node in nodesList) _nodes.Add(node.Text);

                attributeForm.SetNodes(_nodes);
                attributeForm.Show();
                attributeForm.FormClosing += (obj, args) =>
                {
                    TreeNode newNode = new TreeNode();
                    attribute = attributeForm.GetAttributeDictionary();
                    newNode.Text = attribute["Name"];
                    foreach (KeyValuePair<string, string> attr in attribute)
                    {
                        TreeNode childNode = new TreeNode {Text = attr.Key + @"=>" + attr.Value};
                        newNode.Nodes.Add(childNode);
                    }

                    Table table = _app.GetTableByName(_app.GetDatabaseByName(DatabasesTree.SelectedNode.Parent.Text),
                        DatabasesTree.SelectedNode.Text);
                    try
                    {
                        switch (attribute["SQLType"])
                        {
                            case "NVARCHAR":
                                _app.AddDecimalAttribute(table, attribute["Name"],
                                    Convert.ToBoolean(attribute["IsNullable"]));
                                break;
                            case "INT":
                                _app.AddIntegerAttribute(table, attribute["Name"],
                                    Convert.ToBoolean(attribute["IsNullable"]));
                                break;
                            case "STRING":
                                _app.AddStringAttribute(table, attribute["Name"],
                                    Convert.ToBoolean(attribute["IsNullable"]));
                                break;
                            case "FLOAT":
                                _app.AddFloatAttribute(table, attribute["Name"],
                                    Convert.ToBoolean(attribute["IsNullable"]));
                                break;
                        }
                    }
                    catch (ArgumentException exception)
                    {
                        MessageBox.Show(exception.Message);
                    }

                    DatabasesTree.SelectedNode.Nodes.Add(newNode);
                    DatabasesTree.ExpandAll();
                    DatabasesTree.Update();
                };
            }
            else
            {
                MessageBox.Show(@"Выберите таблицу!");
            }
        }

        private void showDatabaseButton_Click(object sender, EventArgs e)
        {
            DatabasesTree.Nodes.Clear();
            IEnumerable<Database> databases = _app.GetAllDatabases().ToList();
            if (!databases.Any())
            {
                MessageBox.Show(@"Вы не создали ни одной базы данных!");
            }
            else
            {
                foreach (Database database in databases)
                {
                    TreeNode dbasesNode = new TreeNode {Text = database.Name};
                    List<Table> tables = _app.GetDatabaseTables(database).ToList();
                    foreach (Table table in tables)
                    {
                        TreeNode tablesNode = new TreeNode {Text = table.Name};
                        IEnumerable<Attribute> attributes = _app.GetTableAttributes(table);
                        foreach (Attribute attribute in attributes)
                        {
                            TreeNode attributeNode = new TreeNode {Text = attribute.Name};

                            //TreeNode attributType = new TreeNode {Text = attribute.SqlType.ToString()};

                            TreeNode attributeDescription = new TreeNode {Text = attribute.Description};

                            TreeNode attributeIsNullable = new TreeNode {Text = attribute.IsNullable.ToString()};

                            TreeNode attributeIsIndexed = new TreeNode();
                            attributeIsIndexed.Text = attribute.IsIndexed.ToString();

                            TreeNode attributeIsPrimaryKey = new TreeNode();
                            attributeIsPrimaryKey.Text = attribute.IsPrimaryKey.ToString();

                            attributeNode.Nodes.Add("Description => " + attributeDescription.Text);
                            attributeNode.Nodes.Add($"Type => {attribute.SqlType}");
                            //attributeNode.Nodes.Add("Type => " + attributType.Text);
                            attributeNode.Nodes.Add("IsNullable => " + attributeIsNullable.Text);
                            attributeNode.Nodes.Add("IsIndexed => " + attributeIsIndexed.Text);
                            attributeNode.Nodes.Add("IsPrimaryKey => " + attributeIsPrimaryKey.Text);

                            tablesNode.Nodes.Add(attributeNode);
                        }

                        dbasesNode.Nodes.Add(tablesNode);
                    }

                    DatabasesTree.Nodes.Add(dbasesNode);
                }

                DatabasesTree.Update();
                DatabasesTree.ExpandAll();
            }
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            //DataGridViewColumn masterColumn = new DataGridViewColumn
            //{
            //    HeaderText = @"Master table",
            //    Name = @"masterAttribute",
            //    CellTemplate = new DataGridViewRowHeaderCell()
            //};
            //DataGridViewColumn slaveColumn = new DataGridViewColumn
            //{
            //    HeaderText = @"Slave table",
            //    Name = @"slaveAttribute",
            //    CellTemplate = new DataGridViewRowHeaderCell()
            //};
            //LinksView.Columns.Add(masterColumn);
            //LinksView.Columns.Add(slaveColumn);
        }

        private void CreateLinkButton_Click(object sender, EventArgs e)
        {
            if (DatabasesTree.SelectedNode != null && DatabasesTree.SelectedNode.Level == 1)
            {
                TreeNode parentNode = DatabasesTree.SelectedNode.Parent;
                Table seletctedTable = _app.GetTableByName(_app.GetDatabaseByName(parentNode.Text),
                    DatabasesTree.SelectedNode.Text);
                List<TreeNode> listOfConnectiableNodes = new List<TreeNode>();
                foreach (TreeNode childNode in parentNode.Nodes)
                    if (childNode.Text != DatabasesTree.SelectedNode.Text)
                        listOfConnectiableNodes.Add(childNode);
                IEnumerable<Link> links =
                    _app.GetDatabaseLinks(_app.GetDatabaseByName(DatabasesTree.SelectedNode.Parent.Text));

                foreach (TreeNode connectiableNode in listOfConnectiableNodes)
                {
                    Table connectedTable =
                        _app.GetTableByName(_app.GetDatabaseByName(parentNode.Text), connectiableNode.Text);
                    foreach (Link link in links)
                        if (link.MasterAttributeId == seletctedTable.Id && link.SlaveAttributeId == connectedTable.Id)
                            listOfConnectiableNodes.Remove(connectiableNode);
                }

                LinkCreationForm linkForm = new LinkCreationForm();
                linkForm.setListOfNodes(listOfConnectiableNodes);
                linkForm.setMasterTable(DatabasesTree.SelectedNode.Text);
                linkForm.Show();

                linkForm.FormClosing += (obj, args) =>
                {
                    Table slaveTable = _app.GetTableByName(_app.GetDatabaseByName(parentNode.Text),
                        linkForm.getSlaveTable());
                    try
                    {
                        _app.AddLink(seletctedTable, slaveTable, linkForm.getCascadeDelete(),
                            linkForm.getCascadeUpdate());
                    }
                    catch (ArgumentException ae)
                    {
                        MessageBox.Show(@"Такая ссылка уже существует." + ae.Message);
                    }
                };
            }
            else
            {
                MessageBox.Show(@"Выберите таблицу!");
            }
        }

        private void ShowLinks_Click(object sender, EventArgs e)
        {
            LinksView.Rows.Clear();
            if (DatabasesTree.SelectedNode != null &&
                DatabasesTree.SelectedNode.Level == 0)
            {
                Database showingDatabase = _app.GetDatabaseByName(DatabasesTree.SelectedNode.Text);
                IEnumerable<Link> links;
                try
                {
                    links = _app.GetDatabaseLinks(showingDatabase).ToList();
                }
                catch (ArgumentException ex)
                {
                    MessageBox.Show(@"Ошибка при получении ссылок. " + ex.Message);
                    return;
                }

                foreach (Link link in links)
                {
                    try
                    {
                        Table tableOne = _app.GetTableById(link.MasterAttributeId);
                        Table tableTwo = _app.GetAttributeTable(_app.GetAttributeById(link.SlaveAttributeId));
                        try
                        {
                            LinksView.Rows.Add(tableOne.Name, tableTwo.Name);
                        }
                        catch (Exception exc)
                        {
                            MessageBox.Show("WTF just happened?!");
                        }
                    }
                    catch (ArgumentNullException except)
                    {
                        MessageBox.Show(@"Нулевая ссылка: " + except.Message);
                    }
                    catch (InvalidOperationException ex)
                    {
                        MessageBox.Show(@"Ошибка при вставке: " + ex.Message);
                    }
                }
            }
            else
            {
                MessageBox.Show(@"Выберите базу данных!");
            }
        }

        private void DeleteDatabaseButton_Click(object sender, EventArgs e)
        {
            if (DatabasesTree.SelectedNode != null &&
                DatabasesTree.SelectedNode.Level == 0)
            {
                try
                {
                    if (_app.IsDatabaseExist(DatabasesTree.SelectedNode.Text))
                    {
                        _app.RemoveDatabase(_app.GetDatabaseByName(DatabasesTree.SelectedNode.Text));
                        DatabasesTree.SelectedNode.Remove();
                    }
                    else
                    {
                        MessageBox.Show(@"Базы с таким именем не существует!");
                    }
                }
                catch (NullReferenceException exception)
                {
                    MessageBox.Show(@"Нулевая ссылка: " + exception.Message);
                }

            }
            else
            {
                MessageBox.Show(@"Выберите базу данных!");
            }
        }

        private void DeleteTableButton_Click(object sender, EventArgs e)
        {
            //Чекнуть существование связи с этой таблицей перед удалением!
            if (DatabasesTree.SelectedNode != null &&
                DatabasesTree.SelectedNode.Level == 1)
            {
                try
                {
                        _app.RemoveTable(_app.GetTableByName(_app.GetDatabaseByName(DatabasesTree.SelectedNode.Parent.Text), DatabasesTree.SelectedNode.Text));
                        DatabasesTree.SelectedNode.Remove();
                }
                catch (ArgumentException exception)
                {
                    MessageBox.Show(@"У таблицы DatabasesTree.SelectedNode.Text имеются связанные таблицы!" + exception.Message);
                }

            }
            else
            {
                MessageBox.Show(@"Выберите таблицу!");
            }
        }

        private void DeleteAttributeButton_Click(object sender, EventArgs e)
        {
            if (DatabasesTree.SelectedNode != null &&
                DatabasesTree.SelectedNode.Level == 2)
            {
                try
                {
                    //жесть, надо бы проверить на работоспосоьность
                    _app.RemoveAttribute(_app.GetAttributeByName(_app.GetTableByName(_app.GetDatabaseByName(DatabasesTree.SelectedNode.Parent.Parent.Text), DatabasesTree.SelectedNode.Parent.Text), DatabasesTree.SelectedNode.Text));
                    DatabasesTree.SelectedNode.Remove();
                }
                catch (NullReferenceException exception)
                {
                    MessageBox.Show(@"Нулевая ссылка: " + exception.Message);
                }

            }
            else
            {
                MessageBox.Show(@"Выберите атрибут!");
            }
        }

        private void DeleteLinkButton_Click(object sender, EventArgs e)
        {
            //if (LinksView.SelectedRows.Count == 0)
            //{

            //    _app.GetLink()
            //        try
            //    {
            //        LinksView.Rows.Add(_app.GetTableById(link.MasterAttributeId).Name, _app.GetAttributeTable(_app.GetAttributeById(link.SlaveAttributeId)).Name);
            //    }
            //    catch (ArgumentNullException except)
            //    {
            //        MessageBox.Show(@"Нулевая ссылка: " + except.Message);
            //    }
            //    catch (InvalidOperationException ex)
            //    {
            //        MessageBox.Show(@"Ошибка при вставке: " + ex.Message);
            //    }
            //}
            //else
            //{
            //    MessageBox.Show(@"Выберите связь для удаления!");
            //}
        }
    }
}