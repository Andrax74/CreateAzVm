using System;
using Microsoft.Azure.Management.Compute.Fluent.Models;
using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.Network.Fluent.Models;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;

namespace CreateAzVm
{
    class Program
    {
        /*
        Info: https://docs.microsoft.com/it-it/dotnet/azure/sdk/resource-management
        Examples: https://github.com/Azure/azure-libraries-for-net#networking
        */
        static void Main(string[] args)
        {
            //Create the management client. This will be used for all the operations
            //that we will perform in Azure.

            try
            {
                var credentials = SdkContext.AzureCredentialsFactory.FromFile("./azureauth.properties");

                // Dati di autenticazione
                var azure = Azure.Configure()
                    .WithLogLevel(HttpLoggingDelegatingHandler.Level.Basic)
                    .Authenticate(credentials)
                    .WithDefaultSubscription();

                //Specifichiamo le variabili con i dati di configurazionee
                var groupName = "alphashop";
                var nsgName = "vm001Nsg";
                var vmName = "vm001";
                var location = Region.USWest2;
                var vNetName = "vm001VNET";
                var vNetAddress = "172.16.0.0/16";
                var subnetName = "vm001Subnet";
                var subnetAddress = "172.16.0.0/24";
                var nicName = "vm001NIC";
                var adminUser = "azureadminuser";
                var publicIPName = "vm001PublicIP";
                //var adminPassword = "Pa$$w0rd!2019";

                //Fase 1 - Creazione del del Resource Group
                Console.WriteLine($"Creating resource group {groupName} ...");
                var resourceGroup = azure.ResourceGroups.Define(groupName)
                    .WithRegion(location)
                    .Create();

                //Fase 2 - Creazione di un nuovo nsg
                Console.WriteLine($"Creating network security group {nsgName} ...");
                var nsg = azure.NetworkSecurityGroups.Define(nsgName)
                    .WithRegion(location)
                    .WithExistingResourceGroup(groupName)
                    .DefineRule("ALLOW-SSH")
                        .AllowInbound()
                        .FromAnyAddress()
                        .FromAnyPort()
                        .ToAnyAddress()
                        .ToPort(22)
                        .WithProtocol(SecurityRuleProtocol.Tcp)
                        .WithPriority(100)
                        .WithDescription("Allow SSH")
                        .Attach()
                    /*   //Parametri Macchina Windows
                    .DefineRule("Allow-RDP")
                        .AllowInbound()
                        .FromAnyAddress()
                        .FromAnyPort()
                        .ToAnyAddress()
                        .ToPort(3389)
                        .WithProtocol(SecurityRuleProtocol.Tcp)
                        .WithPriority(100)
                        .WithDescription("Allow-RDP")
                        .Attach()
                    */
                    .Create();


                //Fase 3 - Creazione Ip Pubblico
                Console.WriteLine($"Creating public IP {publicIPName} ...");
                var publicIP = azure.PublicIPAddresses.Define(publicIPName)
                    .WithRegion(location)
                    .WithExistingResourceGroup(groupName)
                    .Create();
                    
                //Fase 4 - Creazione della Virtual Network
                Console.WriteLine($"Creating virtual network {vNetName} ...");
                var network = azure.Networks.Define(vNetName)
                    .WithRegion(location)
                    .WithExistingResourceGroup(groupName)
                    .WithAddressSpace(vNetAddress)
                    .WithSubnet(subnetName, subnetAddress)
                    .Create();

                //Fase 5 - Creazione dell'interfaccia di rete (NIC)
                Console.WriteLine($"Creating network interface {nicName} ...");
                var nic = azure.NetworkInterfaces.Define(nicName)
                    .WithRegion(location)
                    .WithExistingResourceGroup(groupName)
                    .WithExistingPrimaryNetwork(network)
                    .WithSubnet(subnetName)
                    .WithPrimaryPrivateIPAddressDynamic()
                    .WithExistingPrimaryPublicIPAddress(publicIP)
                    .WithExistingNetworkSecurityGroup(nsg)
                    .Create();

                //Fase 6 - Creazione della VM 
                Console.WriteLine($"Creating virtual machine {vmName} ...");
                azure.VirtualMachines.Define(vmName)
                    .WithRegion(location)
                    .WithExistingResourceGroup(groupName)
                    .WithExistingPrimaryNetworkInterface(nic)

                    // usare "az vm image list --output table" per visualizzare l'elenco delle immagini disponibili
                    //Parametri Macchina Linux
                    .WithLatestLinuxImage("Canonical","UbuntuServer","18.04-LTS")
                    .WithRootUsername(adminUser)
                    .WithSsh("ssh-rsa AAAAB3NzaC1yc2EAAAADAQABAAACAQC+9Q0UNSbSrjo7lpYSL1EZ1U3sn28lMm72qMMn98PD6/kk99F3B2oUb+6iCLNLk/hwvuQC4HhRmERPJfKhmfJTqsPv6tJZXhb2hYWBy288df9Ma+HqDOWgHdBPKyt2xpBUkkAA6lXEn1KrhCfgGUC7t6pdISN9O6JUsktwv3VnQIgz/JCS3FqR0ISzbeA56IJSa9ESAloPVw8Muy2OY8JPNYN7PdMjO7eD//qvX2Ja4F0FRWDJMOjHrvjDihLnrM4F1+RFoz9STvxXPfAr/tbXkhT11X5faGSgi99pkL7Rl4W3GLYq9D+vthN8MNdbTQuhT25CAzLyM+nhmpeiWnS44euYWexiuztDA9Rj0+V5Wgwnq9+Qz9PxmyHdE/4mrSWeFSpmJ/ydIY5avOrdhO899UM6+onsaa9qLjwohq9Xf8w3rYr5fsGBTfGTbiPk1XxiAXGCuyA7erMMrwg7LZcNEYF26K3xwai9CyXduhccv0dto/AzSzJhVTZLeK4bRW1nm2vYPccVcA8E+adGmMyydNHl/A56YqF1mW+hJepA+j/cwXkF1Gr7MN44iycO4GCCaf/u57U35KWtE6lZUVntAT64CoyKSUF++OgvFpNf9WCt7Z18fKV026jdWaJJ5J/lyQgQzMaz3JPI6dYAwOpSzg0MGFUa52TVGeE39ltzLQ== nicola@Win10Nik")

                    //Parametri Macchina Windows
                    //.WithLatestWindowsImage("MicrosoftWindowsServer", "WindowsServer", "2012-R2-Datacenter")
                    //.WithAdminUsername(adminUser)
                    //.WithAdminPassword(adminPassword)

                    .WithComputerName(vmName)
                    .WithSize(VirtualMachineSizeTypes.StandardB1s)
                    .Create();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Errore: " + ex.Message);
            }
            
        }
    }
}

//Eliminazione resource group con tutte le risorse
// az group delete -n alphashop