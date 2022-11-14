out="out"$1
echo "Writing to $out" 
echo "Plotting CCF"
python3 scripts/plot_ccf.py -i $out/ccf.csv -o $out
echo "Plotting Fish Plot"
col=42
smooth=2
pyfish $out/populations.csv $out/parent_tree.csv $out/fish.png -R $col -S $smooth
pyfish $out/populations.csv $out/parent_tree.csv $out/fish_abs.png -R $col -S $smooth -a  
echo "Plotting Parent Graph"
dot -Tpng $out/parent_graph.dot > $out/parent_graph.png
python3 scripts/dot_to_newick.py $out/parent_graph.dot
echo "Plotting Bin Tree"
dot -Tpng $out/bin_tree.dot > $out/bin_tree.png
python3 scripts/dot_to_newick.py $out/bin_tree.dot
echo "plotting Metrics Overview"
python3 scripts/plot_metrics_single_experiment.py --input_folder $out 

