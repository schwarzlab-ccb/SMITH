out="out"$1
echo "Writing to $out" 
echo "Plotting CCF"
python3 scripts/cancer_cell_fraction.py --input_file $out/ccf.csv --output_folder $out
echo "Plotting Fish Plot"
col=42
smooth=2
pyfish $out/populations.csv $out/parent_tree.csv $out/fish.png -R $col -S $smooth
pyfish $out/populations.csv $out/parent_tree.csv $out/fish_abs.png -R $col -S $smooth -a  
echo "Plotting Parent Graph"
dot -Tpng $out/parent_graph.dot > $out/parent_tree.png
#echo "Plotting Population Dynamics"
#python3 scripts/plot_population_dynamics.py --input_folder $out --output_folder $out
echo "plotting Metrics Overview"
python3 scripts/plot_metrics_single_experiment.py --input_folder $out 
#echo "Plotting CN Tracks"
#python3 scripts/plot_cn_tracks.py --input_folder out --output_folder out --fraction
