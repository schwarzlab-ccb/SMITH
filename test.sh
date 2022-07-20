dotnet run -- -C test/sim_params.json
echo "------------------------------------------------------"
echo "- COMPARE FILES --------------------------------------"
echo "------------------------------------------------------"
diff out/parent_graph.dot test/parent_graph.dot
diff out/parent_tree.csv test/parent_tree.csv
diff out/populations.csv test/populations.csv
diff out/sim_params.json test/sim_params.json
diff out/subclones.out test/subclones.out
diff out/summary.csv test/summary.csv  -I "[0-9][0-9].[0-9]*" # Ignore time differences