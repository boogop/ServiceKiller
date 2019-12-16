using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.ServiceProcess;
using rS = ServiceKiller.Properties.Resources;

/*
 * A simple app to search for services and kill them because I was tired of doing that manually 
 * 
 * Only works if you run it as administrator
 * 
 * https://github.com/boogop/ServiceKiller
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
            toolTip1.SetToolTip(btnFind, rS.FindServices);   
            toolTip1.SetToolTip(btnSave, rS.SaveTerm);
            toolTip1.SetToolTip(btnAdd, rS.AddList);
            toolTip1.SetToolTip(btnAll, rS.KillAll);
            toolTip1.SetToolTip(btnClearList, rS.ClearSave);
            toolTip1.SetToolTip(btnKillFaves, rS.KillList);
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

                string[] foo = e.Node.Text.Split(':');
                if (foo.Length == 0) return;

                if (e.Button == MouseButtons.Left)
                {
                    string nodename = foo[0];
                    getServiceDescription(nodename);
                }

                if (e.Button == MouseButtons.Right)
                {
                    string nodename = e.Node.Text;
                    selectedNode = foo[0];// nodename;
                    treeView1.SelectedNode.Name = nodename;
                    contextMenuStrip1.Show();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), rS.AppTitle);
            }
            finally
            {
                lblStatus.Text = "Ready";
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
                    double memsize = clsServices.getMemSize(displayname);
                    displayname += ":  " + memsize.ToString() + "MB";

                    // assign a pic to running/not running
                    int pic = 9;
                    if (stat.ToUpper() == "STOPPED")
                    {
                        if (chkRunning.Checked) continue;
                        pic = 7;
                    }

                    // add child node
                    TreeNode child = new TreeNode(displayname, pic, pic);
                    main.Nodes.Add(child);
                }

                treeView1.Nodes[0].Expand();
                lblStatus.Text = "Ready";
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), rS.AppTitle);
            }
            finally
            {
                lblStatus.Text = "Ready";
            }
        }

        private void startService(string node)
        {
            try
            {
                lblStatus.Text = "Starting " + node;
                Application.DoEvents();

                double timeout = chkNull.numNull(txtTimeout.Text);

                string[] foo = node.Split(':');
                if (foo.Length == 0) return;

                // try to start the service
                clsServices.startService(foo[0], timeout);
                
                if (chkNull.isNull(txtServiceName.Text)) return;

                // refresh the list, use the search term if it's there
                string tofind = txtServiceName.Text.ToUpper();
                findServices(tofind);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), rS.AppTitle);
            }
            finally
            {
                lblStatus.Text = "Ready";
            }
        }

        private void stopService(string node, bool refresh)
        {
            try
            {
                lblStatus.Text = "Stopping " + node;
                Application.DoEvents();

                double timeout = chkNull.numNull(txtTimeout.Text);

                string[] foo = node.Split(':');
                if (foo.Length == 0) return;

                // try to stop the service
                clsServices.stopService(foo[0], timeout);
                
                if (chkNull.isNull(txtServiceName.Text)) return;

                // not sure why I added this arg
                if (!refresh) return;

                // refresh the list, use the search term if it's there
                string tofind = txtServiceName.Text.ToUpper();
                findServices(tofind);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), rS.AppTitle);
            }
            finally
            {
                lblStatus.Text = "Ready";
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

        private void clearPrefs()
        {
            string path = Application.StartupPath + @"\sch.pref";
            System.IO.File.Delete(path);
            lstSearch.Items.Clear();
        }

        private void stopAllServices(bool confirm)
        {
            if (confirm)
            {
                string msg = "Are you sure you want to stop all services?";
                if (MessageBox.Show(msg, "Really?", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.No)
                    return;
            }

            List<string> t = new List<string>();
            try
            {
                foreach (TreeNode n in treeView1.Nodes[0].Nodes)
                {
                    string nodename = n.Text.ToUpper();
                    string[] foo = nodename.Split(':');
                    if (foo.Length == 0) continue;

                    t.Add(foo[0]);                   
                }
                double timeout = chkNull.numNull(txtTimeout.Text);
                clsServices.stopService(t, timeout);

                string tofind = txtServiceName.Text.ToUpper();
                findServices(tofind);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), rS.AppTitle);
            }
            finally
            {
                lblStatus.Text = "Ready";
            }
        }

        private void savePrefs()
        {
            List<string> s = new List<string>();
            for (int i = 0; i < lstSearch.Items.Count; i++)
                s.Add(lstSearch.Items[i].ToString());

            GeneralTools.WriteAnythingToFile(Application.StartupPath, @"\" + "sch.pref", s);

            MessageBox.Show("Preferences saved!", rS.AppTitle);
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
            stopAllServices(true);
        }

        private void btnClearList_Click(object sender, EventArgs e)
        {
            try
            {
                clearPrefs();
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
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

        private void btnKillFaves_Click(object sender, EventArgs e)
        {
            for(int i = 0;i<lstSearch.Items.Count;i++)
            {
                txtServiceName.Text = chkNull.whenNull(lstSearch.Items[i].ToString());
                if (!chkNull.isNull(txtServiceName.Text))
                {
                    btnFind_Click(null, null);
                    stopAllServices(false);
                }
            }
        }
    }
}
