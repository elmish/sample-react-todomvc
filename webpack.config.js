const path = require("path");
const webpack = require("webpack");
const CopyWebpackPlugin = require('copy-webpack-plugin');
const HtmlWebpackPlugin = require('html-webpack-plugin');

const babelOptions = {
    presets: [
      ["@babel/preset-env", {
          "targets": {
              "browsers": ["last 2 versions"]
          },
          "modules": false
      }]
    ]
  };

const out_path = path.resolve('./build');

const isProduction = process.argv.indexOf("-p") >= 0;
console.log("Bundling for " + (isProduction ? "production" : "development") + "...");

const commonPlugins = [
        new HtmlWebpackPlugin({
            filename: './index.html',
            template: './src/index.html'
        }),
        new CopyWebpackPlugin([
            { from: './node_modules/todomvc-app-css/index.css' }
        ])];

module.exports = {
    mode: isProduction ? "production" : "development",
    entry: isProduction ? // We don't use the same entry for dev and production, to make HMR over style quicker for dev env
            { demo: [
                "@babel/polyfill",
                path.resolve('./src/app.fsproj')
            ]}
            : { app: [
                "@babel/polyfill",
                path.resolve('./src/app.fsproj') ]},
    output: {
        path: out_path,
        filename: isProduction ? '[name].[hash].js' : '[name].js'
    },
    optimization : {
        splitChunks: {
            cacheGroups: {
                commons: {
                    test: /[\\/]node_modules[\\/]/,
                    name: "vendors",
                    chunks: "all"
                },
                fable: {
                    test: /[\\/]fable-core[\\/]/,
                    name: "fable",
                    chunks: "all"
                }
            }
        },
    },
    resolve: {
        modules: [
            "node_modules", path.resolve("./node_modules/")
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
                        define: isProduction ? [] : ["DEBUG"],
                        extra: { optimizeWatch: true }
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
            }
        ]
    },
    plugins: isProduction
                ? commonPlugins
                : commonPlugins.concat([
                    new webpack.HotModuleReplacementPlugin(),
                    new webpack.NamedModulesPlugin()
                ])
}; 
