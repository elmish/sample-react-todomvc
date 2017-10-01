var path = require("path");
var fs = require("fs");
var webpack = require("webpack");
var fableUtils = require("fable-utils");
var copyWebpackPlugin = require('copy-webpack-plugin');

function resolve(filePath) {
    return path.join(__dirname, filePath)
}

var babelOptions = fableUtils.resolveBabelOptions({
    presets: [["es2015", { "modules": false }]],
    plugins: ["transform-runtime"]
});

var out_path = resolve('./build');

var isProduction = process.argv.indexOf("-p") >= 0;
console.log("Bundling for " + (isProduction ? "production" : "development") + "...");

var entry = isProduction 
    ? { vendor: [ 
        'react',
        'react-dom',
        'whatwg-fetch',
        ],
        main: "./src/app.fsproj" }
    : resolve('./src/app.fsproj');

var output = isProduction 
    ? { publicPath: "/",
        path: out_path,
        filename: '[chunkhash].[name].js' }
    : { filename: 'main.js',
        path: out_path }

var plugins = isProduction
    ? [
        new webpack.optimize.CommonsChunkPlugin({
            names: ['vendor','manifest'] // Specify the common bundle names.
        }),
        new copyWebpackPlugin([
            { from: 'src/index.html'},
            { from: 'node_modules/todomvc-app-css/index.css' },
            { from: 'node_modules/todomvc-common/base.css' }
        ]),
        function () {
            this.plugin("done", function (stats) {
                var replaceInFile = function (filePath, replacements) {
                    var str = fs.readFileSync(filePath, 'utf8');
                    replacements.forEach(function ({toReplace,replacement}) {
                        var replacer = function (match) {
                            console.log('Replacing in %s: %s => %s', filePath, match, replacement);
                            return './' + replacement;
                        };
                        str = str.replace(new RegExp(toReplace, 'g'), replacer);
                    });
                    fs.writeFileSync(filePath, str);
                };
                var assetsByChunkName = stats.toJson().assetsByChunkName;
                var regexStr = function (chunk) {
                    return '\.\/([a-z0-9]*\.{0,1})' + chunk + '\.js';
                }
                replaceInFile(path.join(out_path, 'index.html'),
                    [ {toReplace: regexStr('main'), replacement: assetsByChunkName.main[0]},
                    {toReplace: regexStr('manifest'), replacement: assetsByChunkName.manifest[0]},
                    {toReplace: regexStr('vendor'), replacement: assetsByChunkName.vendor[0]} ]
                );
            });
        }
        ]
    : [
        new webpack.HotModuleReplacementPlugin(),
        new webpack.NamedModulesPlugin(),
        new copyWebpackPlugin([
            { from: 'src/index.html' },
            { from: 'node_modules/todomvc-app-css/index.css' },
            { from: 'node_modules/todomvc-common/base.css' }
        ])];

module.exports = {
    devtool: "source-map",
    entry: entry,
    output: output,
    resolve: {
        modules: [
            "node_modules", resolve("./node_modules/")
        ]
    },
    devServer: {
        contentBase: out_path,
        port: 8080,
        hot: true,
        inline: true
    },
    module: {
        rules: [
            {
                test: /\.fs(x|proj)?$/,
                use: {
                    loader: "fable-loader",
                    options: {
                        babel: babelOptions,
                        define: isProduction ? [] : ["DEBUG"]
                    }
                }
            },
            {
                test: /\.js$/,
                exclude: /node_modules/,
                use: {
                    loader: 'babel-loader',
                    options: babelOptions
                },
            },
            {
                test: /\.sass$/,
                use: [
                    "style-loader",
                    "css-loader",
                    "sass-loader"
                ]
            }
        ]
    },
    plugins: plugins
};
