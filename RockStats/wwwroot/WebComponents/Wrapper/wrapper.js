"use strict";
var RockStats;
(function (RockStats) {
    var WebComponents;
    (function (WebComponents) {
        var Wrapper = (function (_super) {
            __extends(Wrapper, _super);
            function Wrapper() {
                return _super !== null && _super.apply(this, arguments) || this;
            }
            Wrapper.prototype._isRoute = function (currentRoute, where) {
                if (currentRoute.route === "" && where === "leaderboard")
                    return true;
                else if (currentRoute.route === where)
                    return true;
            };
            Wrapper.prototype._loaderboard = function () {
                this.app.changePath("");
            };
            Wrapper.prototype._transactions = function () {
                this.app.changePath("transactions");
            };
            Wrapper = __decorate([
                Vidyano.WebComponents.WebComponent.register({
                    properties: {
                        currentRoute: Object
                    }
                }, "rockstats")
            ], Wrapper);
            return Wrapper;
        }(Vidyano.WebComponents.WebComponent));
        WebComponents.Wrapper = Wrapper;
    })(WebComponents = RockStats.WebComponents || (RockStats.WebComponents = {}));
})(RockStats || (RockStats = {}));
