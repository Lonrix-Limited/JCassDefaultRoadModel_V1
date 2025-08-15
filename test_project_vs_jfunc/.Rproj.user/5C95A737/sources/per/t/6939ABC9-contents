#-------------------------------------------------------------------------------
#
#  Compares the indexes of elements with triggered treatments in Period 1
#
#-------------------------------------------------------------------------------
rm(list = ls())
library(tidyverse)
library(readxl)
library(clipr)
library(DescTools)

folder <- "cassandra/outputs/"

#-------------------  Mesh Cracks ----------------------------------------------

param <- "para_pdi_rank"
par_type <- "num"


year_min <- 2023
year_max <- 2053


f1 <- paste0(folder, param, "_current_jfunc.csv")
d1 <- read.csv(f1)


f2 <- paste0(folder, param, "_current.csv")
d2 <- read.csv(f2)


years <- seq(year_min, year_max)

get_comparison_set <- function(year) {
  col <- paste0("X", year)
  tmp <- data.frame(elem_index = d1$elem_index, jfunc = d1[, col], c_sharp = d2[, col])
  if (par_type == "txt") {
    tmp$diff <- ifelse(tmp$jfunc == tmp$c_sharp, 0,1)
  } else {
    tmp$diff <- tmp$jfunc - tmp$c_sharp
  }
  
  return(tmp)
}


result <- NULL
for (year in years) {
  
  tmp <- get_comparison_set(year)
  tot_diff <- sum(tmp$diff)  
  
  diffs <- tmp %>% filter(abs(diff) > 0)
  num_diff <- nrow(diffs)
  
  if (is.null(result)) {
    result <- data.frame(year = c(year), tot_diff = c(tot_diff), num_diff = c(num_diff))
  } else {
    tmp <- data.frame(year = c(year), tot_diff = c(tot_diff), num_diff = c(num_diff))
    result <- rbind(result, tmp)
  }
}

result

print(paste0("Total Diff = ", sum(result$tot_diff)))
print(paste0("Rows with Diff = ", sum(result$num_diff)))


dd <- get_comparison_set(2031)
tt <- dd %>% filter(abs(diff) > 0)

# para_adt
# para_hcv
# para_pave_age
# para_pave_remlife
# para_pave_life_ach
# para_hcv_risk
# para_surf_mat
# para_surf_class
# para_surf_cs_flag
# para_surf_cs_or_ac_flag
# para_surf_road_type
# para_surf_thick
# para_surf_layers
# para_surf_func
# para_surf_exp_life
# para_surf_age
# para_surf_life_ach
# para_surf_remain_life
# para_flush_pct
# para_flush_info
# para_edgeb_pct
# para_edgeb_info
# para_scabb_pct
# para_scabb_info
# para_lt_cracks_pct
# para_lt_cracks_info
# para_mesh_cracks_pct
# para_mesh_cracks_info
# para_shove_pct
# para_shove_info
# para_poth_pct
# para_poth_info
# para_rut_increm
# para_rut
# para_naasra_increm
# para_naasra
# para_sdi
# para_pdi
# para_obj_distress
# para_obj_rsl
# para_obj_rutting
# para_obj_naasra
# para_obj_o
# para_obj
# para_obj_auc
# para_maint_cost_perkm
# para_csl_status
# para_csl_flag
# para_is_treated_flag
# para_treat_count
# para_pdi_rank
# para_rut_rank
# para_sdi_rank
# para_sla_rank
