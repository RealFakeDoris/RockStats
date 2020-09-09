"use strict";
var RockStats;
(function (RockStats) {
    var WebComponents;
    (function (WebComponents) {
        WebComponents.flairs_map = {
            "rock": "https://emoji.redditmedia.com/l9rufetdlxd51_t5_m3idt/rock",
            "salamander": "https://emoji.redditmedia.com/b517pvmtlxd51_t5_m3idt/salamander",
            "soon": "https://emoji.redditmedia.com/g4dm01b3mxd51_t5_m3idt/soon",
            "omg": "https://emoji.redditmedia.com/htvyq0opp6d51_t5_m3idt/omg",
            "wve": "https://emoji.redditmedia.com/2pl9xkmspnk51_t5_m3idt/wve",
            "snl": "https://emoji.redditmedia.com/88nfcm4xpnk51_t5_m3idt/snl",
            "skb": "https://emoji.redditmedia.com/at87t5aopnk51_t5_m3idt/skb"
        };
        var LeaderBoard = (function (_super) {
            __extends(LeaderBoard, _super);
            function LeaderBoard() {
                return _super !== null && _super.apply(this, arguments) || this;
            }
            LeaderBoard.prototype.attached = function () {
                return __awaiter(this, void 0, void 0, function () {
                    var _a, list;
                    return __generator(this, function (_b) {
                        switch (_b.label) {
                            case 0:
                                _super.prototype.attached.call(this);
                                return [4, this.app.initialize];
                            case 1:
                                _b.sent();
                                _a = this._setAccounts;
                                return [4, this.app.service.getQuery("2739d6b8-5c14-4c27-9f04-19bf6cdbaf5a")];
                            case 2:
                                _a.apply(this, [_b.sent()]);
                                list = Polymer.dom(this.root).querySelector("#list");
                                list.scrollTarget = this.findParent(function (e) { return e instanceof Vidyano.WebComponents.Scroller; }, list)["scroller"];
                                list.fire("iron-resize");
                                this._setIsBusy(false);
                                return [2];
                        }
                    });
                });
            };
            LeaderBoard.prototype._items = function (items, sort) {
                if (items == null)
                    return [];
                if (!document.baseURI.trimEnd('/').endsWith("/team"))
                    items = items.filter(function (i) { return !i.values.Moderator; });
                else
                    items = items.filter(function (i) { return i.values.Moderator; });
                var index = 0;
                var result;
                if (sort == "balance" || sort == "sent") {
                    var column_1 = sort === "balance" ? "Balance" : "Sent";
                    var columnBN_1 = column_1 + "BigNumber";
                    var div_1 = new BigNumber("1000000000000000000");
                    result = items.filter(function (i) { return i.values.Redditor != null && i.values.Avatar != null; }).map(function (i) {
                        if (!i.values[columnBN_1])
                            i.values[columnBN_1] = (new BigNumber(i.values[column_1])).div(div_1);
                        return i;
                    }).filter(function (i) { return i.values[columnBN_1].greaterThan(0); }).sort(function (a, b) {
                        return a.values[columnBN_1].greaterThan(b.values[columnBN_1]) ? -1 : 1;
                    });
                    var previous_1 = null;
                    result.forEach(function (i) {
                        var value = i.values[columnBN_1];
                        if (previous_1 == null || value.lessThan(previous_1)) {
                            index++;
                            previous_1 = value;
                        }
                        i.values.Rank = index;
                    });
                }
                else {
                    var column_2;
                    switch (sort) {
                        case "in": {
                            column_2 = "TxsIn";
                            break;
                        }
                        case "out": {
                            column_2 = "TxsOut";
                            break;
                        }
                    }
                    var previous_2;
                    result = items.filter(function (i) { return i.values[column_2] > 0; }).map(function (i) {
                        var value = i.values[column_2];
                        if (previous_2 == null || value < previous_2) {
                            index++;
                            previous_2 = value;
                        }
                        i.values.Rank = index;
                        return i;
                    });
                }
                return result;
            };
            LeaderBoard.prototype._select = function (e) {
                this.selectedAccount = e.model.account;
            };
            LeaderBoard.prototype._closeSelectedAccount = function () {
                this.selectedAccount = null;
            };
            LeaderBoard.prototype._computeHasSelectedAccount = function (selectedAccount) {
                return selectedAccount != null;
            };
            LeaderBoard.prototype._address = function (account) {
                return "https://blockexplorer.mainnet.v1.omg.network/address/" + account.values.Address + "?section=transactions";
            };
            LeaderBoard.prototype._flairs = function (flairs) {
                if (flairs == null)
                    return [];
                return flairs.split(":").filter(function (f) { return f != null && f.length > 0; }).map(function (f) { var _a; return (_a = WebComponents.flairs_map[f]) !== null && _a !== void 0 ? _a : null; }).filter(function (f) { return !!f; });
            };
            LeaderBoard.prototype._avatar = function (account) {
                var _a;
                return (_a = account.values.Avatar) === null || _a === void 0 ? void 0 : _a.split("?")[0];
            };
            LeaderBoard.prototype._value = function (account, sort) {
                if (sort !== "balance" && sort !== "sent") {
                    if (sort == "in")
                        return account.values.TxsIn;
                    else if (sort == "out")
                        return account.values.TxsOut;
                }
                var n = account.values[(sort === "balance" ? "Balance" : "Sent") + "BigNumber"];
                if (n.lessThan(10000))
                    return n.round(2);
                else if (n.lessThan(1000000))
                    return n.div(1000).round(2) + "K";
                return n.div(1000000).round(3) + "M";
            };
            LeaderBoard.prototype._isSortActive = function (sort, term) {
                return sort === term;
            };
            LeaderBoard.prototype._sort = function (e) {
                if (this.isBusy)
                    return;
                var sort = e.target.getAttribute("sort");
                if (sort == null)
                    return;
                this._setSort(sort);
            };
            LeaderBoard.prototype._sortChanged = function (sort) {
                return __awaiter(this, void 0, void 0, function () {
                    var columnName;
                    return __generator(this, function (_a) {
                        switch (_a.label) {
                            case 0:
                                if (!this.accounts || this.isBusy)
                                    return [2];
                                switch (sort) {
                                    case "balance": {
                                        columnName = "Balance";
                                        break;
                                    }
                                    case "sent": {
                                        columnName = "Sent";
                                        break;
                                    }
                                    case "in": {
                                        columnName = "TxsIn";
                                        break;
                                    }
                                    case "out": {
                                        columnName = "TxsOut";
                                        break;
                                    }
                                }
                                this.accounts.getColumn(columnName).sort(Vidyano.SortDirection.Descending);
                                _a.label = 1;
                            case 1:
                                _a.trys.push([1, , 3, 4]);
                                this._setIsBusy(true);
                                return [4, this.accounts.search()];
                            case 2:
                                _a.sent();
                                return [3, 4];
                            case 3:
                                this._setIsBusy(false);
                                return [7];
                            case 4: return [2];
                        }
                    });
                });
            };
            LeaderBoard.prototype._showMedal = function (index) {
                return index <= 3;
            };
            LeaderBoard = __decorate([
                Vidyano.WebComponents.WebComponent.register({
                    properties: {
                        accounts: {
                            type: Object,
                            readOnly: true
                        },
                        items: {
                            type: Array,
                            computed: "_items(accounts.items, sort)"
                        },
                        sort: {
                            type: String,
                            readOnly: true,
                            value: "balance",
                            observer: "_sortChanged"
                        },
                        isBusy: {
                            type: Boolean,
                            readOnly: true,
                            value: true
                        },
                        selectedAccount: {
                            type: Object,
                            value: null
                        },
                        hasSelectedAccount: {
                            type: Boolean,
                            computed: "_computeHasSelectedAccount(selectedAccount)",
                            reflectToAttribute: true
                        }
                    },
                    forwardObservers: [
                        "accounts.items"
                    ],
                    mediaQueryAttributes: true
                }, "rockstats")
            ], LeaderBoard);
            return LeaderBoard;
        }(Vidyano.WebComponents.WebComponent));
        WebComponents.LeaderBoard = LeaderBoard;
    })(WebComponents = RockStats.WebComponents || (RockStats.WebComponents = {}));
})(RockStats || (RockStats = {}));
