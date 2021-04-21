library(reshape)
library("ggplot2")
library("gridExtra")

my_data <- read.csv("2021Summary.csv")
my_data <- melt(my_data)
my_data$block <- factor(my_data$variable, 
                       levels = c("B1C0","B1C1","B1C2","B1C3","B1C4","B2C0","B2C1","B2C2","B2C3","B2C4","B3C0","B3C1","B3C2","B3C3","B3C4"),
                       labels = c("B1", "B1", "B1","B1","B1", "B2", "B2", "B2","B2","B2", "B3", "B3", "B3","B3","B3") )

my_data$count <- factor(my_data$variable, 
                        levels = c("B1C0","B1C1","B1C2","B1C3","B1C4","B2C0","B2C1","B2C2","B2C3","B2C4","B3C0","B3C1","B3C2","B3C3","B3C4"),
                        labels = c("C0", "C1", "C2","C3","C4", "C0", "C1", "C2","C3","C4","C0", "C1", "C2","C3","C4") )
str(my_data)
head(my_data)

table(my_data$count, my_data$value)
table(my_data$block, my_data$value)
table(my_data$block, my_data$count)


q <- ggplot(my_data, aes(x=block, y=value)) + 
  geom_boxplot(outlier.colour="red", outlier.shape=8,
               outlier.size=4, alpha=0.3, fill="lightblue")  + 
  theme(legend.position="none") + ggtitle("Block vs. Accuracy") +
  xlab("Block") + ylab("Accuracy (%)")


p <- ggplot(my_data, aes(x=count, y=value)) + 
  geom_boxplot(outlier.colour="red", outlier.shape=8,
               outlier.size=4, alpha=0.3, fill="purple")  + 
  theme(legend.position="none") + ggtitle("Mark Count vs. Accuracy") +
  xlab("Mark Count") + ylab("Accuracy (%)")

grid.arrange(p, q, nrow=1, ncol=2)


res.aov2 <- aov(value ~ block + count, data = my_data)
print(res.aov2)
summary(res.aov2)

# Two-way ANOVA with interaction effect
res.aov3 <- aov(value ~ block * count, data = my_data)
summary(res.aov3)
TukeyHSD(res.aov3, which = "count")


