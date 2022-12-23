# Pull the repository from the Noble et al. 2022 GitHub repository: https://github.com/robjohnnoble/ModesOfEvolution
# Then set the NOBLE_REPO_DIR variable to the path to the repository.
NOBLE_REPO_DIR = "../../../ModesOfEvolution"


library(ape)
library(readr)
library(dplyr)
library(ggmuller)
library(Rgraphviz)
library(ggplot2)
library(gridExtra)
library(demonanalysis)
library(ggrepel)
library(data.table)

move_down_mod <- function(edges, parent) {
  edges <- filter(edges, Parent != 0 | Identity != 0)
  if (!(parent %in% edges$Identity) & !(parent %in% edges$Parent)) 
    stop("Invalid parent.")
  daughters <- filter(edges, Parent == parent)$Identity
  if (length(daughters) == 0) 
    return(parent)
  if (is.factor(daughters)) 
    daughters <- levels(daughters)[daughters]
  return(sort(daughters))
}
get_ITH_from_tree <- function(tree) {
  node <- 0
  clonal <- 0
  for(i in 1:100) {
    node <- move_down_mod(tree, node)
    clonal <- clonal + 1
    if(length(node) > 1) return((dim(tree)[1] - clonal) / clonal)
  }
  return(0)
}


metrics_extended <- function(data) {
  return(c(metrics(data), ITH = get_ITH_from_tree(data)))
}


K153 <- read.csv(paste0(NOBLE_REPO_DIR, "/RealTumourTreesData/TRACERx_Trees/K153.csv"), stringsAsFactors=FALSE)
K255 <- read.csv(paste0(NOBLE_REPO_DIR, "/RealTumourTreesData/TRACERx_Trees/K255.csv"), stringsAsFactors=FALSE)
K448 <- read.csv(paste0(NOBLE_REPO_DIR, "/RealTumourTreesData/TRACERx_Trees/K448.csv"), stringsAsFactors=FALSE)
K252 <- read.csv(paste0(NOBLE_REPO_DIR, "/RealTumourTreesData/TRACERx_Trees/K252.csv"), stringsAsFactors=FALSE)
K136 <- read.csv(paste0(NOBLE_REPO_DIR, "/RealTumourTreesData/TRACERx_Trees/K136.csv"), stringsAsFactors=FALSE)
K153min <- read.csv(paste0(NOBLE_REPO_DIR, "/RealTumourTreesData/TRACERx_Trees/Minimal/K153.csv"), stringsAsFactors=FALSE)
K255min <- read.csv(paste0(NOBLE_REPO_DIR, "/RealTumourTreesData/TRACERx_Trees/Minimal/K255.csv"), stringsAsFactors=FALSE)
K448min <- read.csv(paste0(NOBLE_REPO_DIR, "/RealTumourTreesData/TRACERx_Trees/Minimal/K448.csv"), stringsAsFactors=FALSE)
K252min <- read.csv(paste0(NOBLE_REPO_DIR, "/RealTumourTreesData/TRACERx_Trees/Minimal/K252.csv"), stringsAsFactors=FALSE)
K136min <- read.csv(paste0(NOBLE_REPO_DIR, "/RealTumourTreesData/TRACERx_Trees/Minimal/K136.csv"), stringsAsFactors=FALSE)
CRUK0029 <- read.csv(paste0(NOBLE_REPO_DIR, "/RealTumourTreesData/TRACERx_Trees/CRUK0029.csv"), stringsAsFactors=FALSE)
CRUK0062 <- read.csv(paste0(NOBLE_REPO_DIR, "/RealTumourTreesData/TRACERx_Trees/CRUK0062.csv"), stringsAsFactors=FALSE)
CRUK0065 <- read.csv(paste0(NOBLE_REPO_DIR, "/RealTumourTreesData/TRACERx_Trees/CRUK0065.csv"), stringsAsFactors=FALSE)
CRUK0071 <- read.csv(paste0(NOBLE_REPO_DIR, "/RealTumourTreesData/TRACERx_Trees/CRUK0071.csv"), stringsAsFactors=FALSE)
CRUK0096 <- read.csv(paste0(NOBLE_REPO_DIR, "/RealTumourTreesData/TRACERx_Trees/CRUK0096.csv"), stringsAsFactors=FALSE)
CRUK0029min <- read.csv(paste0(NOBLE_REPO_DIR, "/RealTumourTreesData/TRACERx_Trees/Minimal/CRUK0029.csv"), stringsAsFactors=FALSE)
CRUK0062min <- read.csv(paste0(NOBLE_REPO_DIR, "/RealTumourTreesData/TRACERx_Trees/Minimal/CRUK0062.csv"), stringsAsFactors=FALSE)
CRUK0065min <- read.csv(paste0(NOBLE_REPO_DIR, "/RealTumourTreesData/TRACERx_Trees/Minimal/CRUK0065.csv"), stringsAsFactors=FALSE)
CRUK0071min <- read.csv(paste0(NOBLE_REPO_DIR, "/RealTumourTreesData/TRACERx_Trees/Minimal/CRUK0071.csv"), stringsAsFactors=FALSE)
CRUK0096min <- read.csv(paste0(NOBLE_REPO_DIR, "/RealTumourTreesData/TRACERx_Trees/Minimal/CRUK0096.csv"), stringsAsFactors=FALSE)
PD9849 <- read.csv(paste0(NOBLE_REPO_DIR, "/RealTumourTreesData/YatesEtAl_Trees/PD9849.csv"), stringsAsFactors=FALSE)
PD9694 <- read.csv(paste0(NOBLE_REPO_DIR, "/RealTumourTreesData/YatesEtAl_Trees/PD9694.csv"), stringsAsFactors=FALSE)
PD9852 <- read.csv(paste0(NOBLE_REPO_DIR, "/RealTumourTreesData/YatesEtAl_Trees/PD9852.csv"), stringsAsFactors=FALSE)
PD9849min <- read.csv(paste0(NOBLE_REPO_DIR, "/RealTumourTreesData/YatesEtAl_Trees/Minimal/PD9849.csv"), stringsAsFactors=FALSE)
PD9694min <- read.csv(paste0(NOBLE_REPO_DIR, "/RealTumourTreesData/YatesEtAl_Trees/Minimal/PD9694.csv"), stringsAsFactors=FALSE)
PD9852min <- read.csv(paste0(NOBLE_REPO_DIR, "/RealTumourTreesData/YatesEtAl_Trees/Minimal/PD9852.csv"), stringsAsFactors=FALSE)
AML02 <- read.csv(paste0(NOBLE_REPO_DIR, "/RealTumourTreesData/AML_Trees/AML-02-001.csv"), stringsAsFactors=FALSE)
AML05 <- read.csv(paste0(NOBLE_REPO_DIR, "/RealTumourTreesData/AML_Trees/AML-05-001.csv"), stringsAsFactors=FALSE)
AML16 <- read.csv(paste0(NOBLE_REPO_DIR, "/RealTumourTreesData/AML_Trees/AML-16-001.csv"), stringsAsFactors=FALSE)
AML33 <- read.csv(paste0(NOBLE_REPO_DIR, "/RealTumourTreesData/AML_Trees/AML-33-001.csv"), stringsAsFactors=FALSE)
AML35 <- read.csv(paste0(NOBLE_REPO_DIR, "/RealTumourTreesData/AML_Trees/AML-35-001.csv"), stringsAsFactors=FALSE)
AML55 <- read.csv(paste0(NOBLE_REPO_DIR, "/RealTumourTreesData/AML_Trees/AML-55-001.csv"), stringsAsFactors=FALSE)
AML73 <- read.csv(paste0(NOBLE_REPO_DIR, "/RealTumourTreesData/AML_Trees/AML-73-001.csv"), stringsAsFactors=FALSE)
AML77 <- read.csv(paste0(NOBLE_REPO_DIR, "/RealTumourTreesData/AML_Trees/AML-77-001.csv"), stringsAsFactors=FALSE)
UMM059 <- read.csv(paste0(NOBLE_REPO_DIR, "/RealTumourTreesData/DuranteEtAl_Trees/UMM059.csv"), stringsAsFactors=FALSE)
UMM061 <- read.csv(paste0(NOBLE_REPO_DIR, "/RealTumourTreesData/DuranteEtAl_Trees/UMM061.csv"), stringsAsFactors=FALSE)
UMM062 <- read.csv(paste0(NOBLE_REPO_DIR, "/RealTumourTreesData/DuranteEtAl_Trees/UMM062.csv"), stringsAsFactors=FALSE)
UMM063 <- read.csv(paste0(NOBLE_REPO_DIR, "/RealTumourTreesData/DuranteEtAl_Trees/UMM063.csv"), stringsAsFactors=FALSE)
UMM064 <- read.csv(paste0(NOBLE_REPO_DIR, "/RealTumourTreesData/DuranteEtAl_Trees/UMM064.csv"), stringsAsFactors=FALSE)
UMM065 <- read.csv(paste0(NOBLE_REPO_DIR, "/RealTumourTreesData/DuranteEtAl_Trees/UMM065.csv"), stringsAsFactors=FALSE)
UMM066 <- read.csv(paste0(NOBLE_REPO_DIR, "/RealTumourTreesData/DuranteEtAl_Trees/UMM066.csv"), stringsAsFactors=FALSE)
UMM069 <- read.csv(paste0(NOBLE_REPO_DIR, "/RealTumourTreesData/DuranteEtAl_Trees/UMM069.csv"), stringsAsFactors=FALSE)
MED001 <- read.csv(paste0(NOBLE_REPO_DIR, "/RealTumourTreesData/ZhangEtAl_Trees/MED001.csv"), stringsAsFactors=FALSE)
MED012 <- read.csv(paste0(NOBLE_REPO_DIR, "/RealTumourTreesData/ZhangEtAl_Trees/MED012.csv"), stringsAsFactors=FALSE)
MED023 <- read.csv(paste0(NOBLE_REPO_DIR, "/RealTumourTreesData/ZhangEtAl_Trees/MED023.csv"), stringsAsFactors=FALSE)
MED024 <- read.csv(paste0(NOBLE_REPO_DIR, "/RealTumourTreesData/ZhangEtAl_Trees/MED024.csv"), stringsAsFactors=FALSE)
MED027 <- read.csv(paste0(NOBLE_REPO_DIR, "/RealTumourTreesData/ZhangEtAl_Trees/MED027.csv"), stringsAsFactors=FALSE)
MED034 <- read.csv(paste0(NOBLE_REPO_DIR, "/RealTumourTreesData/ZhangEtAl_Trees/MED034.csv"), stringsAsFactors=FALSE)
TN1 <- read.csv(paste0(NOBLE_REPO_DIR, "/RealTumourTreesData/MinussiEtAl_Trees/TN1.csv"), stringsAsFactors=FALSE)
TN2 <- read.csv(paste0(NOBLE_REPO_DIR, "/RealTumourTreesData/MinussiEtAl_Trees/TN2.csv"), stringsAsFactors=FALSE)
TN3 <- read.csv(paste0(NOBLE_REPO_DIR, "/RealTumourTreesData/MinussiEtAl_Trees/TN3.csv"), stringsAsFactors=FALSE)
TN4 <- read.csv(paste0(NOBLE_REPO_DIR, "/RealTumourTreesData/MinussiEtAl_Trees/TN4.csv"), stringsAsFactors=FALSE)
TN5 <- read.csv(paste0(NOBLE_REPO_DIR, "/RealTumourTreesData/MinussiEtAl_Trees/TN5.csv"), stringsAsFactors=FALSE)
TN6 <- read.csv(paste0(NOBLE_REPO_DIR, "/RealTumourTreesData/MinussiEtAl_Trees/TN6.csv"), stringsAsFactors=FALSE)
TN7 <- read.csv(paste0(NOBLE_REPO_DIR, "/RealTumourTreesData/MinussiEtAl_Trees/TN7.csv"), stringsAsFactors=FALSE)
TN8 <- read.csv(paste0(NOBLE_REPO_DIR, "/RealTumourTreesData/MinussiEtAl_Trees/TN8.csv"), stringsAsFactors=FALSE)
real_points <- as.data.frame(rbind(metrics_extended(K153),
                                   metrics_extended(K255),
                                   metrics_extended(K448),
                                   metrics_extended(K252),
                                   metrics_extended(K136),
                                   metrics_extended(K153min),
                                   metrics_extended(K255min),
                                   metrics_extended(K448min),
                                   metrics_extended(K252min),
                                   metrics_extended(K136min),
                                   metrics_extended(CRUK0029),
                                   metrics_extended(CRUK0062),
                                   metrics_extended(CRUK0065),
                                   metrics_extended(CRUK0071),
                                   metrics_extended(CRUK0096),
                                   metrics_extended(CRUK0029min),
                                   metrics_extended(CRUK0062min),
                                   metrics_extended(CRUK0065min),
                                   metrics_extended(CRUK0071min),
                                   metrics_extended(CRUK0096min),
                                   metrics_extended(PD9852),
                                   metrics_extended(PD9849),
                                   metrics_extended(PD9694),
                                   metrics_extended(PD9852min),
                                   metrics_extended(PD9849min),
                                   metrics_extended(PD9694min),
                                   metrics_extended(AML02),
                                   metrics_extended(AML05),
                                   metrics_extended(AML16),
                                   metrics_extended(AML33),
                                   metrics_extended(AML35),
                                   metrics_extended(AML55),
                                   metrics_extended(AML73),
                                   metrics_extended(AML77),
                                   metrics_extended(UMM059),
                                   metrics_extended(UMM061),
                                   metrics_extended(UMM062),
                                   metrics_extended(UMM063),
                                   metrics_extended(UMM064),
                                   metrics_extended(UMM065),
                                   metrics_extended(UMM066),
                                   metrics_extended(UMM069),
                                   metrics_extended(MED001),
                                   metrics_extended(MED012),
                                   metrics_extended(MED023),
                                   metrics_extended(MED024),
                                   metrics_extended(MED027),
                                   metrics_extended(MED034),
                                   metrics_extended(TN1),
                                   metrics_extended(TN2),
                                   metrics_extended(TN3),
                                   metrics_extended(TN4),
                                   metrics_extended(TN5),
                                   metrics_extended(TN6),
                                   metrics_extended(TN7),
                                   metrics_extended(TN8)
))

real_points$tumour <- c(rep(c("K153", "K255", "K448", "K252", "K136"), 2), 
                        rep(c("CRUK0029", "CRUK0062", "CRUK0065", "CRUK0071", "CRUK0096"), 2), 
                        rep(c("PD9852", "PD9849", "PD9694"), 2), 
                        c("AML-02-001","AML-05-001","AML-16-001","AML-33-001","AML-35-001","AML-55-001","AML-73-001","AML-77-001"), 
                        c("UMM059","UMM061","UMM062","UMM063","UMM064","UMM065","UMM066","UMM069"), 
                        c("MED001","MED012","MED023","MED024","MED027","MED034"), 
                        c("TN1","TN2","TN3","TN4","TN5","TN6","TN7","TN8"))
real_points$tumourshort <- c(rep(c("K153", "K255", "K448", "K252", "K136"), 2), 
                             rep(c("C29", "C62", "C65", "C71", "C96"), 2), 
                             rep(c("P852", "P849", "P694"), 2), 
                             c("A02","A05","A16","A33","A35","A55","A73","A77"), 
                             c("U59","U61","U62","U63","U64","U65","U66","U69"), 
                             c("M01","M12","M23","M24","M27","M34"), 
                             c("TN1","TN2","TN3","TN4","TN5","TN6","TN7","TN8"))
real_points$dataset <- c(rep("kidney", 10), rep("lung", 10), rep("breast", 6), rep("AML", 8), rep("uveal", 8), rep("mesothelioma", 6), rep("breast_SC", 8))
real_points$minimal <- c(rep(0, 5), rep(1, 5), rep(0, 5), rep(1, 5), rep(0, 3), rep(1, 3), rep(0, 8), rep(0, 8), rep(0, 6), rep(0, 8))

write.csv(data.frame(real_points), paste0(NOBLE_REPO_DIR, '/real_data.csv'))