﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.ServiceProcess;
using System.Management;

/*
 * A simple app to search for services and kill them because I was tired of doing that manually 
 * 
 * Only works if you run it as administrator
 */

namespace ServiceKiller
{
    public partial class Form1 : Form
    {
        string selectedNode = "";

        public Form1()
        {
            InitializeComponent();

            readPrefs();
            toolTip1.SetToolTip(btnFind, "Find matching services");
            toolTip1.SetToolTip(btnSave, "Save search terms");
            toolTip1.SetToolTip(btnAdd, "Add to list");
            toolTip1.SetToolTip(btnAll, "Kill all listed services");
        }


        #region treeview

        private void treeView1_MouseDown(object sender, MouseEventArgs e)
        {

        }

        private void treeView1_NodeMouseClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            try
            {
                treeView1.SelectedNode = e.Node;

                if (e.Button == MouseButtons.Left)
                {
                    string nodename = e.Node.Text;
                    getServiceDescription(nodename);
                }

                if (e.Button == MouseButtons.Right)
                {
                    string nodename = e.Node.Text;
                    selectedNode = nodename;
                    treeView1.SelectedNode.Name = nodename;
                    contextMenuStrip1.Show();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "Service Killer");
            }
            finally
            {
                lblStatus.Text = "Ready...";
            }
        }

        private void contextMenuStrip1_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {
            // handle the right click menu
            ToolStripItem item = e.ClickedItem;
            string action = item.Text.ToUpper();
            switch (action)
            {
                case "START":
                    startService(selectedNode);
                    break;
                case "STOP":
                    stopService(selectedNode, true);
                    break;
                default:
                    break;
            }
        }

        #endregion


        #region methods

        private void findServices(string tofind)
        {
            try
            {
                lblStatus.Text = "Searching...";
                Application.DoEvents();
                treeView1.Nodes.Clear();
                ServiceController[] services;

                clsServices.getAllServices(out services);
               
                // add main node
                TreeNode main = new TreeNode("Services", 11, 11);
                treeView1.Nodes.Add(main);

                // loop through the services
                for (int i = 0; i < services.Length; i++)
                {
                    string displayname = services[i].DisplayName.ToUpper();

                    // find the one we want. If * then get them all
                    if (tofind != "*")
                        if (displayname.IndexOf(tofind) == -1) continue;
                   
                    string stat = clsServices.getStatus(services[i]);

                    // assign a pic to running/not running
                    int pic = 9;
                    if (stat.ToUpper() == "STOPPED")
                        pic = 7;

                    // add child node
                    TreeNode child = new TreeNode(displayname, pic, pic);
                    main.Nodes.Add(child);
                }

                treeView1.Nodes[0].Expand();
                lblStatus.Text = "Ready";
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "Service Killer");
            }
            finally
            {
                lblStatus.Text = "Ready...";
            }
        }

        private void startService(string node)
        {
            try
            {
                lblStatus.Text = "Starting " + node;
                Application.DoEvents();
                double timeout = chkNull.numNull(txtTimeout.Text);

                // try to start the service
                clsServices.startService(node, timeout);
                
                if (chkNull.isNull(txtServiceName.Text)) return;

                // refresh the list, use the search term if it's there
                string tofind = txtServiceName.Text.ToUpper();
                findServices(tofind);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "Service Killer");
            }
            finally
            {
                lblStatus.Text = "Ready...";
            }
        }

        private void stopService(string node, bool refresh)
        {
            try
            {
                lblStatus.Text = "Stopping " + node;
                Application.DoEvents();
                double timeout = chkNull.numNull(txtTimeout.Text);

                // try to stop the service
                clsServices.stopService(node, timeout);
                
                if (chkNull.isNull(txtServiceName.Text)) return;

                // not sure why I added this arg
                if (!refresh) return;

                // refresh the list, use the search term if it's there
                string tofind = txtServiceName.Text.ToUpper();
                findServices(tofind);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "Service Killer");
            }
            finally
            {
                lblStatus.Text = "Ready...";
            }
        }

        private void getServiceDescription(string servicename)
        {
            txtDescription.Text = clsServices.getServiceDescription(servicename);            
        }

        private void readPrefs()
        {
            // read saved search terms
            List<string> s = new List<string>();
            string path = Application.StartupPath + @"\sch.pref";
            GeneralTools.ReadAnythingFromFile(path, ref s);
            if (s.Count == 0) return;
            for (int i = 0; i < s.Count; i++)
                lstSearch.Items.Add(s[i]);

        }

        private void stopAllServices()
        {
            string foo = "Are you sure you want to stop all services?";
            if (MessageBox.Show(foo, "Really?", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.No)
                return;

            List<string> t = new List<string>();
            try
            {
                foreach (TreeNode n in treeView1.Nodes[0].Nodes)
                {
                    string nodename = n.Text.ToUpper();
                    t.Add(nodename);                   
                }
                double timeout = chkNull.numNull(txtTimeout.Text);
                clsServices.stopService(t, timeout);

                string tofind = txtServiceName.Text.ToUpper();
                findServices(tofind);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "Service Killer");
            }
            finally
            {
                lblStatus.Text = "Ready...";
            }
        }

        private void savePrefs()
        {
            List<string> s = new List<string>();
            for (int i = 0; i < lstSearch.Items.Count; i++)
                s.Add(lstSearch.Items[i].ToString());

            GeneralTools.WriteAnythingToFile(Application.StartupPath, @"\" + "sch.pref", s);

            MessageBox.Show("Preferences saved!", "Service Killer");
        }

        #endregion


        #region buttons

        private void btnFind_Click(object sender, EventArgs e)
        {
            if (chkNull.isNull(txtServiceName.Text)) return;
            string tofind = txtServiceName.Text.ToUpper();

            findServices(tofind);

        }

        private void txtServiceName_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)Keys.Enter)
            {
                btnFind_Click(null, null);
            }
        }

        private void btnAll_Click(object sender, EventArgs e)
        {
            stopAllServices();
        }


        private void btnSave_Click(object sender, EventArgs e)
        {
            if (lstSearch.Items.Count == 0) return;

            savePrefs();

        }

        private void btnAdd_Click(object sender, EventArgs e)
        {
            if (chkNull.isNull(txtSearch.Text)) return;
            lstSearch.Items.Add(txtSearch.Text);
        }

        #endregion


        #region other rando stuff

        private void txtSearch_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)Keys.Enter)
            {
                // add the term to the listbox
                btnAdd_Click(null, null);
                txtSearch.Text = "";
            }
        }

        private void lstSearch_Click(object sender, EventArgs e)
        {
            // if a saved search term is clicked, find the service
            txtServiceName.Text = chkNull.whenNull(lstSearch.SelectedItem);
            if (!chkNull.isNull(txtServiceName.Text))
                btnFind_Click(null, null);
        }

        #endregion


    }
}
