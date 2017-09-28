using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using System.IO;
using Newtonsoft.Json;

namespace KrobotkinDiscord.Modules {
    public class Echo : Module {
        public override async void ParseMessageAsync(Channel channel, Message message) {
            if (message.Text.StartsWith("!")) {
                foreach (var echo in Config.INSTANCE.echoCommands) {
                    if (echo.challenge == message.Text.Substring(1).Trim()) {
                        if (echo.server_id == 0) {
                            echo.server_id = Program.PRIMARY_SERVER_ID;
                            Config.INSTANCE.Commit();
                        }
                        if (echo.server_id == message.Server.Id) {
                            await message.Channel.SendMessage(echo.response);
                        }
                    }
                }
            }
        }
        public override void InitiateClient(DiscordClient _client) {

            _client.GetService<CommandService>().CreateGroup("echo", egp => {
                egp.CreateCommand("add")
                .Parameter("challenge")
                .Parameter("response")
                .Do(e => {
                    if (Config.INSTANCE.GetPermissionLevel(e.User, e.Server) > 0) {
                        EchoCommand ec = new EchoCommand {
                            challenge = e.Args[0],
                            response = e.Args[1],
                            server_id = e.Server.Id
                        };
                        if(Config.INSTANCE.echoCommands.Contains(ec)) {
                            e.Channel.SendMessage("Cannot add echo !" + ec.challenge + "... already exists");
                            return;
                        }
                        Config.INSTANCE.echoCommands.Add(ec);
                        Config.INSTANCE.Commit();
                        e.Channel.SendMessage("Added echo !" + ec.challenge + " : " + ec.response);
                    } else {
                        e.Channel.SendMessage("Sorry, you don't have permission to do that.");
                    }
                });
                egp.CreateCommand("remove")
                .Parameter("challenge")
                .Do(e => {
                    if (Config.INSTANCE.GetPermissionLevel(e.User, e.Server) >= 2) {
                        foreach (EchoCommand c in Config.INSTANCE.echoCommands) {
                            if (c.challenge == e.Args[0]) {
                                Config.INSTANCE.echoCommands.Remove(c);
                                break;
                            }
                        }
                        e.Channel.SendMessage($"The echo {e.Args[0]} has been removed.");
                        Config.INSTANCE.Commit();
                    }
                });
                egp.CreateCommand("list-old")
                .Do(async e => {
                    if (Config.INSTANCE.GetPermissionLevel(e.User, e.Server) >= 0) {
                        await e.Channel.SendMessage($"Please wait, compiling echo list. :clock2:");

                        // Compile echolist.html file
                        StreamWriter sw = new StreamWriter("echolist-old.html", false);
                        sw.Write(@"<!DOCTYPE html>
                        <html>
                        <head>
                            <title>Echo List</title>
                            <meta charset='utf-8'>
                            <style>
                            html, body{
                                margin: 0; padding: 0;
                            }

                            body{
                                background-color: #36393E;
                                color: #eee;
                                font-family: Whitney, Helvetica Neue, Helvetica, Arial, sans-serif;
                            }

                            h1{
                                text-align: center;
                            }

                            a{
                                text-decoration: none;
                                color: #46B7E4;
                            }

                            a:hover{
                                text-decoration: underline;
                            }

                            .echos{
                                margin: 40px auto;
                                max-width: 1400px;
                            }

                            .echo{
                                border-top: 1px solid #555;
                                padding-top: 25px;
                                margin-top: 25px;
                            }

                            .title{
                                font-weight: bold;
                                margin-bottom: 5px;
                            }

                            .content{
                                white-space: pre-wrap;
                                color: #aaa;
                            }
                            </style>
                        </head>
                        <body>
                            <h1>Echo List</h1>
                            <div class='echos'>
                        ");
                        foreach (EchoCommand ec in Config.INSTANCE.echoCommands) {
                            if (ec.server_id == e.Server.Id) {
                                if (ec.response.StartsWith("http://") || ec.response.StartsWith("https://")) {
                                    sw.WriteLine($"<div class='echo'><div class='title'>{ec.challenge}</div><div class='content'><a href='{ec.response}' target='_blank'>link</a></div></div>");
                                } else {
                                    sw.WriteLine($"<div class='echo'><div class='title'>{ec.challenge}</div><div class='content'>{ec.response}</div></div>");
                                }
                            }
                        }
                        sw.Write(@"
                            </div>
                            <script>
                            </script>
                        </body>
                        </html>");
                        sw.Close();

                        await e.Channel.SendMessage("List compiled! :robot: :tools:");
                        await e.Channel.SendFile("echolist-old.html");
                    }
                });
                egp.CreateCommand("list")
                .Do(async e => {
                    if (Config.INSTANCE.GetPermissionLevel(e.User, e.Server) >= 0) {
                        await e.Channel.SendMessage($"Please wait, compiling echo list. :clock2:");

                        // Compile echolist.html file
                        StreamWriter sw = new StreamWriter("echolist.html", false);
                        sw.Write(@"<!DOCTYPE html>
                        <html>
                        <head>
                            <title>Echo List</title>
                            <meta charset='utf-8'>
                            <script src='https://ajax.googleapis.com/ajax/libs/angularjs/1.6.4/angular.min.js'></script>
                            <style>
                            html, body{
                                margin: 0; padding: 0;
                            }

                            body{
                                background-color: #36393E;
                                color: #eee;
                                font-family: Whitney, Helvetica Neue, Helvetica, Arial, sans-serif;
                            }
                            
                            .clearfix:after {
                                content:' ';
                                display:block;
                                clear:both;
                            }
                            
                            .col-6{
                                float: left;
                                width: 50%;
                                padding: 8px;
                                box-sizing: border-box;
                            }
                            
                            .text-left{ text-align: left; }
                            .text-center{ text-align: center; }
                            .text-right{ text-align: right; }
                            
                            .container{
                                max-width: 1400px;
                                margin-left: auto;
                                margin-right: auto;
                                box-sizing: border-box;
                            }
                            
                            nav{
                                position: fixed;
                                top: 0;
                                width: 100%;
                                height: 50px;
                                background-color: #1E2124;
                            }
                            
                            input[type=text]{
                                background-color: #303338;
                                border: 1px solid #222427;
                                color: #ccc;
                                border-radius: 2px;
                                padding: 6px 12px;
                                
                                transition: all .2s;
                                -webkit-transition: all .2s;
                                -moz-transition: all .2s;
                                -o-transition: all .2s;
                                -ms-transition: all .2s;
                            }
                            
                            input[type=text]:hover, input[type=text]:active, input[type=text]:focus{
                                background-color: #313339;
                                border-color: #040405;
                                box-shadow: none;
                            }
                            
                            input[type=text]:active, input[type=text]:focus{
                                border-color: #7289da;
                            }
                            

                            h1, h2, h3, h4, h5, h6{
                                margin: 0;
                            }

                            a{
                                text-decoration: none;
                                color: #46B7E4;
                            }

                            a:hover{
                                text-decoration: underline;
                            }

                            .echos{
                                margin-top: 15px;
                                padding: 8px;
                            }

                            .echo{
                                border-top: 1px solid #555;
                                padding-top: 25px;
                                margin-top: 25px;
                            }

                            .title{
                                font-weight: bold;
                                margin-bottom: 5px;
                            }

                            .content{
                                color: #aaa;
                            }
                            
                            .content .content_text{
                                white-space: pre-wrap;
                            }
                            
                            .content img{
                                max-width: 350px;
                                max-height: 150px;
                            }
                            </style>
                        </head>
                        <body ng-app='EchoApp' ng-controller='EchoController as self'>
                            <nav>
                                <div class='container clearfix'>
                                    <div class='col-6'>
                                        <h1>Echo List</h1>
                                    </div>
                                    <div class='col-6 text-right'>
                                        <input ng-model='self.search_input' type='text' placeholder='Search...' size='25'>
                                    </div>
                                </div>
                            </nav>
                            <div class='echos container'>
                                <div class='echo' ng-repeat='echo in self.echos | filter:self.search_input'>
                                    <div class='title'>{{echo.title}}</div>
                                    <div class='content'>
                                        <a ng-if='echo.is_link' ng-href='{{echo.content}}' target='_blank'>
                                            <span ng-if='!echo.is_image'>link: {{echo.content}}</span>
                                            <img ng-if='echo.is_image' ng-src='{{echo.content}}'>
                                        </a>
                                        <div ng-if='!echo.is_link' class='content_text'>{{echo.content}}</div>
                                    </div>
                                </div>
                            </div>
                            
                            <script>
                            angular.module('EchoApp', [])
                                   .factory('EchoData', EchoData)
                                   .controller('EchoController', EchoController);
                            
                            function EchoData(){
                                var echos = 
                        ");

                        // write out echo data
                        List<EchoCommand> echos = new List<EchoCommand>();
                        foreach (EchoCommand ec in Config.INSTANCE.echoCommands){
                            if (ec.server_id == e.Server.Id){
                                echos.Add(ec);
                            }
                        }

                        String json = JsonConvert.SerializeObject(echos);
                        sw.Write(json + ";\n");

                        sw.Write(@"
                                var image_formats = ['jpg', 'jpeg', 'png', 'gif'];

                                //echos = JSON.parse(echos);

                                // process echo data
                                for (var i = 0; i < echos.length; i++){
                                    e = echos[i];
                                    e.content = e.response;
                                    e.title = e.challenge;
                                    e.tags = [];
                                    
                                    // check if link
                                    var lower = e.content.toLowerCase();
                                    if (lower.startsWith('http://') || lower.startsWith('https://')){
                                        e.is_link = true;
                                        
                                        // check if image link
                                        if (image_formats.indexOf(lower.split('.').pop()) >= 0){
                                            e.is_image = true;
                                            e.tags.push('image');
                                        } else {
                                            e.tags.push('link');
                                        }
                                    }
                                }
                                
                                return echos;
                            }
                            
                            function EchoController($scope, EchoData){
                                var self = this;
                                self.echos = EchoData;
                                self.search_input = '';
                            }
                            </script>
                        </body>
                        </html>
                            ");
                        sw.Close();

                        await e.Channel.SendMessage("List compiled! :robot: :tools:");
                        await e.Channel.SendFile("echolist.html");
                    }
                });
            });
        }
    }
}
