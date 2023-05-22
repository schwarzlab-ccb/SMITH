out=$1
[[ -z "$out" ]] && { out="./out"; }

echo "Plotting the folder $out" 

if [[ -f "$out/populations.csv" && -f "$out/parent_tree.csv" ]]; then
    echo "Plotting Fish Plot"
    col=42
    smooth=2
    pyfish $out/populations.csv $out/parent_tree.csv $out/fish.png -R $col -S $smooth
    pyfish $out/populations.csv $out/parent_tree.csv $out/fish_abs.png -R $col -S $smooth -a  
else
    echo "File $out/populations.csv or $out/parent_tree.csv does not exist. Skipping fish plot."
fi

echo "Plotting Clone Tree"
dot -Tpng $out/clone_tree.dot > $out/clone_tree.png
