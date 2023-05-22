out=$1

[[ -z "$out" ]] && { echo "Parameter 1 is empty" ; exit 1; }

echo "Writing to $out" 

cd "./out/$out"
for d in */ ; do
    cd $d
    echo "Plotting Fish Plot"
    col=42
    smooth=2
    pyfish populations.csv parent_tree.csv fish.png -R $col -S $smooth
    pyfish populations.csv parent_tree.csv fish_abs.png -R $col -S $smooth -a  
    echo "Plotting Clone Tree"
    dot -Tpng clone_tree.dot > clone_tree.png
    cd ..
done
cd ..