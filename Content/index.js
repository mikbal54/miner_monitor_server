var servesAddress = "http://localhost:9000"
var refreshTime = 3000;
var shortUpdateTime = 60;
var activeMinerSelectionSeconds = 9

angular.module('minerMonitorApp', [])
  .controller('collapsibleController', function ($scope) {

      var minerMonitor = this;

      minerMonitor.shortUpdateData = []

      minerMonitor.activeMiners = [
          //{
          //    index: 0,
          //    name: "developer",
          //    algo: "ethash",
          //    hashrate: 142.5,
          //},
          // {
          //     index: 1,
          //     name: "tester",
          //     algo: "equihash",
          //     hashrate: 155
          // }
      ]

      minerMonitor.passiveMiners = [

      ]

      minerMonitor.filterActiveMiners = function () {

          
          var seconds = Math.floor((new Date).getTime()/1000);
          var inlastseconds = _.filter(minerMonitor.shortUpdateData, function (item) {
              if (seconds - item.time > activeMinerSelectionSeconds)
                  return true
          });

          minerMonitor.activeMiners = _.uniq(inlastseconds, false, function (item) {
              return item.name;
          });

          var b = 2
      }


      minerMonitor.updateStats = function () {

          minerMonitor.getMinerStats(shortUpdateTime, function (result) {
              $scope.$apply();
          });

          
      }

      minerMonitor.getMinerStats = function (seconds, extraCall) {

          $.ajax({
              url: servesAddress +"/minerStats/"+seconds ,
              success: function (result) {
                  minerMonitor.shortUpdateData = result
                  activeMiners = minerMonitor.filterActiveMiners()
                  extraCall(result)
              },
              dataType: "json"
          });

      }

      minerMonitor.totalHashrates = {}

      minerMonitor.totalHashrateString = function () {
          minerMonitor.calculateTotalHashrate()
          var str = "";
          for (var a in minerMonitor.totalHashrates) {
              str += a;
              str += "= ";
              str += minerMonitor.totalHashrates[a];
              str += ", ";
          }
          str = str.substring(0, str.length - 2);
          return str
      }

      minerMonitor.totalActiveMiners = function () {

          var seconds = Math.floor((new Date).getTime()/1000);
          // actives in the last 30 seconds
          var activeMiners = _.pluck(_(minerMonitor.shortUpdateData).filter(function (val) {
              if (seconds - val.time < activeMinerSelectionSeconds)
                  return val;
          }));
          activeMiners = _.uniq(activeMiners)

          
          return activeMiners.length; //TODO: calculate active miners
      }

      minerMonitor.totalMiners = function () {
          var uniqueMiners = _.pluck(minerMonitor.shortUpdateData, "name");
          uniqueMiners = _.uniq(uniqueMiners)
          return uniqueMiners.length
      }

      minerMonitor.calculateTotalHashrate = function () {

          minerMonitor.totalHashrates = {}
          for (var a in minerMonitor.miners) {

              if( minerMonitor.miners[a].algo in minerMonitor.totalHashrates )
                  minerMonitor.totalHashrates[minerMonitor.miners[a].algo] += minerMonitor.miners[a].hashrate
              else
                  minerMonitor.totalHashrates[minerMonitor.miners[a].algo] = minerMonitor.miners[a].hashrate

          }

      }


  });

